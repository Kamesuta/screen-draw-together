using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using FireSharp.Core;
using FireSharp.Core.Config;
using SIPSorcery.Net;

namespace WebRTCFirebaseSignaling
{
    /// <summary>
    /// <see cref="WebRTCFirebaseSignalingPeer"/> のビルダークラス
    /// </summary>
    public class WebRTCFirebaseSignalingConnector
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
        public WebRTCFirebaseSignalingConnector(string ourID)
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
        public async Task<WebRTCFirebaseSignalingPeer> StartAsGuest(string theirID)
        {
            // 引数の検証
            if (string.IsNullOrWhiteSpace(theirID)) throw new ArgumentNullException(nameof(theirID));

            // Firebaseクライアントを作成します
            var firebaseClient = new FirebaseClient(FirebaseConfig);
            // 接続リクエストを送信
            string content = JsonSerializer.Serialize(OurID);
            await firebaseClient.SetAsync($"rooms/{theirID}/join", content).ConfigureAwait(false);
            // WebRTCピアを作成します
            var pc = await CreatePeerConnection().ConfigureAwait(false);

            // シグナリングオブジェクトを作成します
            var handle = new WebRTCFirebaseSignalingPeer(firebaseClient, pc, OurID, false, theirID);
            // 終了時にFirebaseクライアントを解放
            handle.OnDispose += firebaseClient.Dispose;

            Debug.WriteLine($"[{OurID} -> {theirID}]: Starting as guest.");
            // シグナリングサーバーからのメッセージを受信します
            await handle.ReceiveFromFirebase().ConfigureAwait(false);

            // シグナリングオブジェクトを返します
            return handle;
        }

        /// <summary>
        /// ホストとして新しいWebRTCピア接続を作成し、シグナリングサーバーからの受信を開始します
        /// シグナリングサーバーから接続リクエストを受信した場合、オファーを受信し、アンサーを投稿します
        /// </summary>
        public async Task<WebRTCFirebaseSignalingHost> StartAsHost()
        {
            // Firebaseクライアントを作成します
            var firebaseClient = new FirebaseClient(FirebaseConfig);

            // ルームをリセットします
            await firebaseClient.DeleteAsync($"rooms/{OurID}").ConfigureAwait(false);
            var unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            await firebaseClient.SetAsync($"rooms/{OurID}/open", unixTimestamp).ConfigureAwait(false);

            // ゲスト待ち受けオブジェクトを作成
            var host = new WebRTCFirebaseSignalingHost(firebaseClient, OurID);
            // 終了時にFirebaseクライアントを解放
            host.OnDispose += firebaseClient.Dispose;
            // 接続が来た場合
            host.OnConnectRequest += async (theirID) =>
            {
                // WebRTCピアを作成します
                var pc = await CreatePeerConnection().ConfigureAwait(false);

                // シグナリングオブジェクトを作成します
                var handle = new WebRTCFirebaseSignalingPeer(firebaseClient, pc, OurID, true, theirID);

                Debug.WriteLine($"[{theirID} -> {OurID}]: Connection request received, starting signaling.");
                // シグナリングサーバーからのメッセージを受信します
                await handle.ReceiveFromFirebase().ConfigureAwait(false);

                // シグナリングオブジェクトを追加します
                host.PeerList.Add(handle);
            };

            Debug.WriteLine($"[{OurID}]: Starting as host.");
            // シグナリングサーバーからのメッセージを受信します
            await host.ReceiveFromFirebase().ConfigureAwait(false);

            // ゲスト待ち受けオブジェクトを返します
            return host;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class WebRTCFirebaseSignalingHost : IDisposable
    {
        /// <summary>
        /// このピアが使用する任意のID
        /// </summary>
        public string OurID { get; }

        /// <summary>
        /// このピアが属するルームの任意のID
        /// </summary>
        public string RoomID => OurID;

        /// <summary>
        /// Firebaseクライアント
        /// </summary>
        public FirebaseClient FirebaseClient { get; set; }

        /// <summary>
        /// 接続ピアリスト
        /// </summary>
        public List<WebRTCFirebaseSignalingPeer> PeerList { get; set; } = new List<WebRTCFirebaseSignalingPeer>();

        /// <summary>
        /// 接続リクエストがあった場合
        /// </summary>
        public event Action<string> OnConnectRequest = delegate { };

        /// <summary>
        /// ルーム終了時に行う処理
        /// </summary>
        public event Action OnDispose = delegate { };

        // コンストラクタ
        internal WebRTCFirebaseSignalingHost(FirebaseClient firebaseClient, string ourID)
        {
            // プロパティ初期化
            FirebaseClient = firebaseClient;
            OurID = ourID;
        }


        public void Dispose()
        {
            // 終了イベント
            OnDispose();

            GC.SuppressFinalize(this);
        }

        // シグナリングサーバーからのメッセージを受信します
        internal async Task ReceiveFromFirebase()
        {
            // 接続メッセージを待機します
            await FirebaseClient.OnAsync(
                $"rooms/{RoomID}/join",
                added: (s, args, context) => OnConnectRequest(args.Data),
                changed: (s, args, context) => OnConnectRequest(args.Data)
            ).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Firebase Realtime Databaseを介してシグナリングを実行します
    /// </summary>
    public class WebRTCFirebaseSignalingPeer : IDisposable
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
        internal WebRTCFirebaseSignalingPeer(FirebaseClient firebaseClient, RTCPeerConnection pc, string ourID, bool isHost, string theirID)
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
        }

        // 切断
        public void Dispose()
        {
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
            Debug.WriteLine($"Connection state changed to {state}.");

            // シグナリングが完了(接続が確立された or 切断された)場合、シグナリングを終了します
            if (!(state == RTCPeerConnectionState.@new || state == RTCPeerConnectionState.connecting))
            {
                Debug.WriteLine($"Exiting signaling as connection state is now {state}.");
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
                Debug.WriteLine($"[-> ICE]: {cand.ToShortString()}.");
                // ICEメッセージを送信します
                await SendToSignalingServer(cand.toJSON(), WebRTCSignalTypesEnum.ice).ConfigureAwait(false);
            }
        }

        // 新しいWebRTCピア接続を作成し、シグナリングサーバーにSDPオファーを送信します
        private async Task SendOffer()
        {
            Debug.WriteLine($"[-> SDP]: {RTCSdpType.offer}");
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
                var res = await FirebaseClient.SetAsync($"rooms/{RoomID}/signal/{(IsHost ? TheirID : OurID)}/{(IsHost ? "host" : "guest")}/{sendType}", content).ConfigureAwait(false);
                Debug.WriteLine($"[-> Firebase]: rooms/{RoomID}/signal/{(IsHost ? TheirID : OurID)}/{(IsHost ? "host" : "guest")}/{sendType} {res.StatusCode}");
            }
            catch (TaskCanceledException)
            {
                // シグナリングが完了/失敗したとき、リクエストをキャンセルする
                Debug.WriteLine("Canceled signaling request as signaling is done or failed.");
            }
        }

        // シグナリングサーバーからのメッセージを受信します
        internal async Task ReceiveFromFirebase()
        {
            // ICEメッセージを待機します
            await FirebaseClient.OnAsync(
                $"rooms/{RoomID}/signal/{(IsHost ? TheirID : OurID)}/{(!IsHost ? "host" : "guest")}/{WebRTCSignalTypesEnum.ice}",
                added: (s, args, context) => OnIceMessage(args.Data),
                changed: (s, args, context) => OnIceMessage(args.Data)
            ).ConfigureAwait(false);

            // SDPメッセージを待機します
            await FirebaseClient.OnAsync(
                $"rooms/{RoomID}/signal/{(IsHost ? TheirID : OurID)}/{(!IsHost ? "host" : "guest")}/{WebRTCSignalTypesEnum.sdp}",
                added: async (s, args, context) => await OnSdpMessage(args.Data).ConfigureAwait(false),
                changed: async (s, args, context) => await OnSdpMessage(args.Data).ConfigureAwait(false)
            ).ConfigureAwait(false);

            // ゲストの場合
            if (!IsHost)
            {
                // オファーを送信します
                await SendOffer().ConfigureAwait(false);
            }
        }

        // シグナリングサーバーからのICEメッセージを受信します
        private void OnIceMessage(string signal)
        {
            // ICEメッセージを解析します
            if (RTCIceCandidateInit.TryParse(signal, out var iceCandidateInit))
            {
                // このピアにICEメッセージを追加します
                Debug.WriteLine($"[<- ICE]: {iceCandidateInit.candidate}");
                PeerConnection.addIceCandidate(iceCandidateInit);
            }
            else
            {
                // 未知のICEメッセージ
                Debug.WriteLine($"Unrecognised ICE candidate message: {signal}");
            }
        }

        // シグナリングサーバーからのSDPメッセージを受信します
        private async Task OnSdpMessage(string signal)
        {
            // SDPメッセージを解析します
            if (RTCSessionDescriptionInit.TryParse(signal, out var descriptionInit))
            {
                Debug.WriteLine($"[<- SDP]: {descriptionInit.type}");
                //Debug.WriteLine(descriptionInit.sdp);

                // リモートピアのSDPを設定します
                var result = PeerConnection.setRemoteDescription(descriptionInit);

                if (result != SetDescriptionResultEnum.OK)
                {
                    // リモートピアのSDPを設定できない場合
                    Debug.WriteLine($"Failed to set remote description, {result}.");
                    // このピアを閉じます
                    PeerConnection.Close("failed to set remote description");
                }
                else if (descriptionInit.type == RTCSdpType.offer)
                {
                    Debug.WriteLine($"[-> SDP]: {RTCSdpType.answer}");
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
                Debug.WriteLine($"Unrecognised SDP message: {signal}");
            }
        }
    }
}