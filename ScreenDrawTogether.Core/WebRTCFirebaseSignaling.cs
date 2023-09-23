using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FireSharp.Core;
using FireSharp.Core.Config;
using SIPSorcery.Net;

namespace ScreenDrawTogether.Core;

/// <summary>
/// Firebaseを使ったWebRTCシグナリングを行います
/// </summary>
public static partial class WebRTCFirebaseSignaling
{
    /// <summary>
    /// シグナリング用ロガー
    /// </summary>
    private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Signaling");

    /// <summary>
    /// <see cref="SignalingPeer"/> のビルダークラス
    /// </summary>
    public partial class SignalingConnector
    {
        /// <summary>
        /// このピアが使用する任意のID
        /// </summary>
        public string OurID { get; }

        /// <summary>
        /// Firebaseの設定
        /// </summary>
        public FirebaseConfig? FirebaseConfig { get; set; }

        /// <summary>
        /// 新しいWebRTCピア接続を作成するために使用される関数デリゲート
        /// </summary>
        public Func<Task<RTCPeerConnection>> CreatePeerConnection { get; set; } = () => Task.FromResult(new RTCPeerConnection(null));

        /// <summary>
        /// デフォルト コンストラクタ
        /// </summary>
        /// <param name="ourID">このピアが使用する任意のID</param>
        public SignalingConnector(string ourID)
        {
            // 引数の検証
            if (string.IsNullOrWhiteSpace(ourID)) throw new ArgumentNullException(nameof(ourID));

            // プロパティを設定します
            OurID = ourID;
        }

        /// <summary>
        /// ゲストとして新しいWebRTCピア接続を作成し、シグナリングサーバーからの受信を開始します
        /// オファーを投稿し、アンサーを受信します
        /// </summary>
        /// <param name="theirID">リモートピアが使用する任意のID</param>
        public async Task<SignalingPeer> StartAsGuest(string theirID)
        {
            // 引数の検証
            if (string.IsNullOrWhiteSpace(theirID)) throw new ArgumentNullException(nameof(theirID));

            // Firebaseクライアントを作成します
            var firebaseClient = new FirebaseClient(FirebaseConfig);

            // メッセージ受信イベントを登録
            Logger.Info($"[{OurID[..8]} -> {theirID[..8]}]: Sending connection requested.");

            // セッションを初期化します
            Logger.Info($"[{OurID[..8]} -> Firebase]: DELETE rooms/{theirID}/signal/{OurID}");
            await firebaseClient.DeleteAsync($"rooms/{theirID}/signal/{OurID}").ConfigureAwait(false);
            var unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            Logger.Info($"[{OurID[..8]} -> Firebase]: PUT rooms/{theirID}/signal/{OurID}/open");
            await firebaseClient.SetAsync($"rooms/{theirID}/signal/{OurID}/open", unixTimestamp).ConfigureAwait(false);

            // WebRTCピアを作成します
            var pc = await CreatePeerConnection().ConfigureAwait(false);

            // 接続が来た場合
            Action<string, string> onSignal = (path, data) => Logger.Info($"[{OurID[..8]} <- Firebase]: ON_ASYNC rooms/{theirID}/signal/{OurID}/host{path}");

            // シグナリングオブジェクトを作成します
            var handle = new SignalingPeer(firebaseClient, pc, OurID, false, theirID);
            // 終了時
            handle.OnDispose += () =>
            {
                // メッセージ受信イベントを解除
                onSignal -= handle.OnSignal;
                // Firebaseクライアントを解放
                firebaseClient.Dispose();
            };

            // シグナリングサーバーからのメッセージを受信します
            Logger.Info($"[{OurID[..8]} -> Firebase]: Starting as guest. (rooms/{theirID}/signal/{OurID}/host)");
            onSignal += handle.OnSignal;
            await firebaseClient.OnAsync(
                $"rooms/{theirID}/signal/{OurID}/host",
                added: (s, args, context) => onSignal(args.Path, args.Data),
                changed: (s, args, context) => onSignal(args.Path, args.Data)
            ).ConfigureAwait(false);

            // シグナリングオブジェクトを返します
            return handle;
        }

        /// <summary>
        /// ホストとして新しいWebRTCピア接続を作成し、シグナリングサーバーからの受信を開始します
        /// シグナリングサーバーから接続リクエストを受信した場合、オファーを受信し、アンサーを投稿します
        /// </summary>
        public async Task<SignalingHost> StartAsHost()
        {
            // Firebaseクライアントを作成します
            var firebaseClient = new FirebaseClient(FirebaseConfig);

            // ルームを初期化します
            Logger.Info($"[{OurID[..8]} -> Firebase]: DELETE rooms/{OurID}");
            await firebaseClient.DeleteAsync($"rooms/{OurID}").ConfigureAwait(false);
            var unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            Logger.Info($"[{OurID[..8]} -> Firebase]: PUT rooms/{OurID}/open");
            await firebaseClient.SetAsync($"rooms/{OurID}/open", unixTimestamp).ConfigureAwait(false);

            // 接続が来た場合
            Action<string, string> onSignal = (path, data) => Logger.Info($"[{OurID[..8]} <- Firebase]: ON_ASYNC rooms/{OurID}{path}");

            async void OnHostSignal(string path, string data)
            {
                // /open のパスにマッチするかどうか判定
                var matchOpen = SignalOpenRegex().Match(path);
                if (matchOpen.Success)
                {
                    // パスのパラメーターを取得
                    var guestID = matchOpen.Groups[1].Value;

                    // WebRTCピアを作成します
                    var pc = await CreatePeerConnection().ConfigureAwait(false);

                    // シグナリングオブジェクトを作成します
                    var handle = new SignalingPeer(firebaseClient, pc, OurID, true, guestID);

                    // シグナリングサーバーからのメッセージを受信します
                    void OnSignal(string path, string data)
                    {
                        // /signal のパスにマッチするかどうか判定
                        var matchSignal = SignalGuestRegex().Match(path);
                        if (matchSignal.Success)
                        {
                            // パスのパラメーターを取得
                            var signalGuestID = matchSignal.Groups[1].Value;
                            var signalType = matchSignal.Groups[2].Value;

                            // guestIDが一致したシグナルのみ受信
                            if (guestID != signalGuestID) return;

                            // シグナルを処理します
                            handle.OnSignal($"/{signalType}", data);
                        }
                    };

                    // メッセージ受信イベントを登録
                    Logger.Info($"[{OurID[..8]} <- {guestID[..8]}]: Connection request received, starting signaling.");
                    onSignal += OnSignal;
                    // 終了時
                    handle.OnDispose += async () =>
                    {
                        // メッセージ受信イベントを解除
                        onSignal -= OnSignal;
                        // セッション削除
                        await firebaseClient.DeleteAsync($"rooms/{OurID}/signal/{guestID}").ConfigureAwait(false);
                    };

                    // オファーを送信します
                    await handle.SendOffer().ConfigureAwait(false);
                }
            };

            // ゲスト待ち受けオブジェクトを作成
            var host = new SignalingHost(firebaseClient, OurID);
            // 終了時
            host.OnDispose += async () =>
            {
                // メッセージ受信イベントを解除
                onSignal -= OnHostSignal;
                // ルーム削除
                await firebaseClient.DeleteAsync($"rooms/{OurID}").ConfigureAwait(false);
                // Firebaseクライアントを解放
                firebaseClient.Dispose();
            };

            // シグナリングサーバーからのメッセージを受信します
            Logger.Info($"[{OurID[..8]} -> Firebase]: Starting as host. (rooms/{OurID})");
            onSignal += OnHostSignal;
            await firebaseClient.OnAsync(
                $"rooms/{OurID}",
                added: (s, args, context) => onSignal(args.Path, args.Data),
                changed: (s, args, context) => onSignal(args.Path, args.Data)
            ).ConfigureAwait(false);

            // ゲスト待ち受けオブジェクトを返します
            return host;
        }

        [GeneratedRegex("/signal/(\\w+)/open")]
        private static partial Regex SignalOpenRegex();

        [GeneratedRegex("/signal/(\\w+)/guest/(\\w+)")]
        private static partial Regex SignalGuestRegex();
    }

    /// <summary>
    /// ルームを開き、接続を待機します
    /// 寿命: ルーム開始時からルーム終了時まで。ルームを終了してもPeerConnectionの接続はそのまま続く。
    /// </summary>
    public class SignalingHost : IDisposable
    {
        /// <summary>
        /// このピアが使用する任意のID
        /// </summary>
        public string OurID { get; }

        /// <summary>
        /// Firebaseクライアント
        /// </summary>
        public FirebaseClient FirebaseClient { get; set; }

        /// <summary>
        /// ルーム終了時に行う処理
        /// </summary>
        public event Action OnDispose = delegate { };

        // コンストラクタ
        internal SignalingHost(FirebaseClient firebaseClient, string ourID)
        {
            // プロパティ初期化
            FirebaseClient = firebaseClient;
            OurID = ourID;

            Logger.Info($"[{OurID[..8]}]: Signaling host initialized.");
        }

        // ルーム終了処理
        public void Dispose()
        {
            Logger.Info($"[{OurID[..8]}]: Signaling host disposed.");

            // 終了イベント
            OnDispose();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Firebase Realtime Databaseを介してシグナリングを実行します
    /// 寿命: シグナリング開始時からシグナリング完了時まで。シグナリングが完了してもPeerConnectionの接続はそのまま続く。
    /// </summary>
    public class SignalingPeer : IDisposable
    {
        /// <summary>
        /// このピアが使用する任意のID
        /// </summary>
        public string OurID { get; }

        /// <summary>
        /// クライアントがホストかどうか
        /// </summary>
        public bool IsHost { get; }

        /// <summary>
        /// リモートピアが使用する任意のID
        /// </summary>
        public string TheirID { get; }

        /// <summary>
        /// このピアが属するルームの任意のID
        /// </summary>
        public string RoomID => IsHost ? OurID : TheirID;

        /// <summary>
        /// Firebaseクライアント
        /// </summary>
        public FirebaseClient FirebaseClient { get; set; }

        /// <summary>
        /// WebRTCセッションのピア接続
        /// </summary>
        public RTCPeerConnection PeerConnection { get; set; }

        /// <summary>
        /// シグナリング終了時に行う処理 (1～2回呼ばれます)
        /// </summary>
        public event Action OnDispose = delegate { };

        // コンストラクタ
        internal SignalingPeer(FirebaseClient firebaseClient, RTCPeerConnection pc, string ourID, bool isHost, string theirID)
        {
            // プロパティ初期化
            OurID = ourID;
            IsHost = isHost;
            TheirID = theirID;
            FirebaseClient = firebaseClient;
            PeerConnection = pc;

            // イベントを登録します
            PeerConnection.onconnectionstatechange += RTCPeerConnection_onconnectionstatechange;
            PeerConnection.onicecandidate += RTCPeerConnection_onicecandidate;

            Logger.Info($"[{OurID[..8]} -- {TheirID[..8]}]: Signaling peer initialized.");
        }

        // 切断
        public void Dispose()
        {
            Logger.Info($"[{OurID[..8]} -- {TheirID[..8]}]: Signaling peer disposed.");

            // イベントを解除します
            PeerConnection.onconnectionstatechange -= RTCPeerConnection_onconnectionstatechange;
            PeerConnection.onicecandidate -= RTCPeerConnection_onicecandidate;

            // 終了イベント
            OnDispose();

            GC.SuppressFinalize(this);
        }

        // 接続状態が変更されたときに発生します
        private void RTCPeerConnection_onconnectionstatechange(RTCPeerConnectionState state)
        {
            Logger.Info($"[{OurID[..8]} -- {TheirID[..8]}]: Connection state changed to {state}.");

            // シグナリングが完了(接続が確立された or 切断された)場合、シグナリングを終了します
            if (!(state == RTCPeerConnectionState.@new || state == RTCPeerConnectionState.connecting))
            {
                Logger.Info($"[{OurID[..8]} -- {TheirID[..8]}]: Exiting signaling as connection state is now {state}.");
                // シグナリングを終了します
                Dispose();
            }
        }

        // ICE候補が生成されたときに発生します
        private async void RTCPeerConnection_onicecandidate(RTCIceCandidate cand)
        {
            // ホストの候補は常にSDPオファーやアンサーに含まれているため、追加の送信は不要です
            if (cand.type != RTCIceCandidateType.host)
            {
                Logger.Info($"[{OurID[..8]} -> {TheirID[..8]}]: ICE {cand.ToShortString()}.");
                // ICEメッセージを送信します
                await SendToSignalingServer(cand.toJSON(), WebRTCSignalTypesEnum.ice).ConfigureAwait(false);
            }
        }

        // 新しいWebRTCピア接続を作成し、シグナリングサーバーにSDPオファーを送信します
        internal async Task SendOffer()
        {
            Logger.Info($"[{OurID[..8]} -> {TheirID[..8]}]: SDP {RTCSdpType.offer}");
            // オファーを作成します
            var offerSdp = PeerConnection.createOffer();
            // オファーを設定します
            await PeerConnection.setLocalDescription(offerSdp).ConfigureAwait(false);
            // オファーを送信します
            await SendToSignalingServer(offerSdp.toJSON(), WebRTCSignalTypesEnum.sdp).ConfigureAwait(false);
        }

        // シグナリングサーバーにメッセージを送信します
        private async Task SendToSignalingServer(string jsonStr, WebRTCSignalTypesEnum sendType)
        {
            // JSON文字列をシリアライズします
            string content = JsonSerializer.Serialize(jsonStr);
            try
            {
                // シグナリングサーバーにデータを送信します
                Logger.Info($"[{OurID[..8]} -> Firebase]: PUT rooms/{RoomID}/signal/{(IsHost ? TheirID : OurID)}/{(IsHost ? "host" : "guest")}/{sendType}");
                await FirebaseClient.SetAsync($"rooms/{RoomID}/signal/{(IsHost ? TheirID : OurID)}/{(IsHost ? "host" : "guest")}/{sendType}", content).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // シグナリングが完了/失敗したとき、リクエストをキャンセルする
                Logger.Info($"[{OurID[..8]} -- {TheirID[..8]}]: Canceled signaling request as signaling is done or failed.");
            }
        }

        // シグナリングサーバーからのメッセージを受信時
        internal async void OnSignal(string path, string signal)
        {
            // パスにマッチするかどうか判定
            switch (path)
            {
                case "/ice":
                    OnIceMessage(signal);
                    break;
                case "/sdp":
                    await OnSdpMessage(signal);
                    break;
            }
        }

        // シグナリングサーバーからのICEメッセージを受信します
        private void OnIceMessage(string signal)
        {
            // ICEメッセージを解析します
            if (RTCIceCandidateInit.TryParse(signal, out var iceCandidateInit))
            {
                // このピアにICEメッセージを追加します
                Logger.Info($"[{OurID[..8]} <- {TheirID[..8]}]: ICE {iceCandidateInit.candidate}");
                PeerConnection.addIceCandidate(iceCandidateInit);
            }
            else
            {
                // 未知のICEメッセージ
                Logger.Warn($"[{OurID[..8]} -- {TheirID[..8]}]: Unrecognised ICE candidate message: {signal}");
            }
        }

        // シグナリングサーバーからのSDPメッセージを受信します
        private async Task OnSdpMessage(string signal)
        {
            // SDPメッセージを解析します
            if (RTCSessionDescriptionInit.TryParse(signal, out var descriptionInit))
            {
                Logger.Info($"[{OurID[..8]} <- {TheirID[..8]}]: SDP {descriptionInit.type}");
                //Logger.Info(descriptionInit.sdp);

                // リモートピアのSDPを設定します
                var result = PeerConnection.setRemoteDescription(descriptionInit);

                if (result != SetDescriptionResultEnum.OK)
                {
                    // リモートピアのSDPを設定できない場合
                    Logger.Warn($"[{OurID[..8]} -- {TheirID[..8]}]: Failed to set remote description, {result}.");
                    // このピアを閉じます
                    PeerConnection.Close("Failed to set remote description");
                }
                else if (descriptionInit.type == RTCSdpType.offer)
                {
                    Logger.Info($"[{OurID[..8]} -> {TheirID[..8]}]: SDP {RTCSdpType.answer}");
                    // リモートピアがオファーを送信した場合
                    var answerSdp = PeerConnection.createAnswer();
                    // アンサーを設定します
                    await PeerConnection.setLocalDescription(answerSdp).ConfigureAwait(false);
                    // アンサーを送信します
                    await SendToSignalingServer(answerSdp.toJSON(), WebRTCSignalTypesEnum.sdp).ConfigureAwait(false);
                }
            }
            else
            {
                // 未知のSDPメッセージ
                Logger.Warn($"[{OurID[..8]} -- {TheirID[..8]}]: Unrecognised SDP message: {signal}");
            }
        }
    }
}