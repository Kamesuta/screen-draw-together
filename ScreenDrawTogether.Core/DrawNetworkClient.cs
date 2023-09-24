using FireSharp.Core.Config;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreenDrawTogether.Core;

/// <summary>
/// ネットワーク接続
/// </summary>
public abstract class DrawNetworkClient : IDisposable
{
    /// <summary>
    /// ネットワーク用ロガー
    /// </summary>
    private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Network");

    /// <summary>
    /// 接続情報
    /// </summary>
    public DrawNetworkRoutingInfo RoutingInfo { get; private set; }

    /// <summary>
    /// 認証情報
    /// </summary>
    public DrawNetworkAuth Auth { get; private set; }

    /// <summary>
    /// シグナリングコネクター
    /// </summary>
    public WebRTCFirebaseSignaling.SignalingConnector Connector { get; }

    /// <summary>
    /// シグナリング中かどうか
    /// </summary>
    public abstract bool IsSignaling { get; }

    /// <summary>
    /// 接続中のピア
    /// </summary>
    public List<RTCPeerConnection> PeerConnections { get; } = new();

    /// <summary>
    /// データチャネル
    /// </summary>
    public List<RTCDataChannel> DataChannels { get; } = new();

    /// <summary>
    /// 接続時のイベント
    /// </summary>
    public event Action OnConnected = delegate { };

    /// <summary>
    /// クライアントが終了したときのイベント
    /// </summary>
    public event Action OnDispose = delegate { };

    /// <summary>
    /// 切断済みかどうか
    /// </summary>
    public event Action<RTCPeerConnectionState> OnHostClosed = delegate { };

    /// <summary>
    /// メッセージを受信したときのイベント
    /// </summary>
    public event OnDataChannelMessageDelegate OnMessage = delegate { };

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="routingInfo">接続情報</param>
    /// <param name="auth">認証情報</param>
    private DrawNetworkClient(DrawNetworkRoutingInfo routingInfo, DrawNetworkAuth auth)
    {
        RoutingInfo = routingInfo;
        Auth = auth;

        // シグナリングコネクターを作成
        Connector = CreateSignaler();
    }

    // ファイナライズ
    public virtual void Dispose()
    {
        // 終了時のイベントを発火
        OnDispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// シグナリングを開始する
    /// ホストの場合: ルームの待ち受けを開始
    /// ゲストの場合: ルームに参加
    /// </summary>
    public abstract Task StartSignaling();

    /// <summary>
    /// シグナリングを停止する
    /// 既に接続中のピアには影響しない
    /// ホストの場合: ルームの待ち受けを停止
    /// ゲストの場合: シグナリング中の場合は中止、既に接続中の場合はなにもしない
    /// </summary>
    public abstract void StopSignaling();

    /// <summary>
    /// シグナリングコネクターを作成する
    /// </summary>
    /// <param name="routingInfo">接続情報</param>
    /// <param name="auth">認証情報</param>
    /// <param name="dataChannels">データのチャネル</param>
    /// <returns></returns>
    private WebRTCFirebaseSignaling.SignalingConnector CreateSignaler()
    {
        // NAT環境下でも接続できるように、STUNサーバーを使用して接続を初期化
        var config = new RTCConfiguration
        {
            iceServers = new List<RTCIceServer>()
            {
                new RTCIceServer()
                {
                    urls = RoutingInfo.RelayUrls,
                    username = RoutingInfo.RelayUsername,
                    credential = RoutingInfo.RelaySecret,
                },
            }
        };

        // Firebase Realtime Database の設定を作成
        var firebaseConfig = new FirebaseConfig()
        {
            BasePath = RoutingInfo.DatabasePath,
            AuthSecret = Auth.ClientIdToken,
        };

        // シグナリングコネクターを作成
        var connector = new WebRTCFirebaseSignaling.SignalingConnector(Auth.ClientId)
        {
            FirebaseConfig = firebaseConfig,
            CreatePeerConnection = () => CreatePeerConnection(config),
        };

        return connector;
    }

    /// <summary>
    /// P2P接続を作成、初期化する
    /// </summary>
    /// <param name="config">P2P設定</param>
    /// <returns>P2P接続</returns>
    private async Task<RTCPeerConnection> CreatePeerConnection(RTCConfiguration config)
    {
        // P2P接続を作成
        var peerConnection = new RTCPeerConnection(config);
        PeerConnections.Add(peerConnection);

        // データチャネルのラベル
        var dataChannelLabel = "data_channel";
        Logger.Info($"Adding data channel with label '{dataChannelLabel}'");
        // データチャネルのオプション
        var chatChannelOption = new RTCDataChannelInit()
        {
            ordered = true,
            negotiated = true,
            id = 0,
        };
        // データチャネルを作成
        var chatChannel = await peerConnection.createDataChannel(dataChannelLabel, chatChannelOption);
        DataChannels.Add(chatChannel);

        // データチャネルのイベントを設定
        chatChannel.onmessage += (RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data) =>
        {
            foreach (var channel in DataChannels)
            {
                // ホストは通信を他のピアにリレーする
                if (channel != dc)
                {
                    channel.send(data);
                }
            }

            // メッセージを受信したときのイベントを発火
            OnMessage(dc, protocol, data);
        };
        // データチャネルが接続されたときのイベントを設定
        chatChannel.onopen += () =>
        {
            Logger.Info($"Data channel '{chatChannel.label}' open.");
            // 接続時のイベントを発火
            OnConnected();
        };

        // データチャネルの接続状態が変化したときのイベントを設定
        peerConnection.onconnectionstatechange += (state) =>
        {
            // シグナリングが完了(接続が確立された or 切断された)場合、シグナリングを終了します
            if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed || state == RTCPeerConnectionState.disconnected)
            {
                Logger.Info($"Exiting connection as connection state is now {state}.");
                // シグナリングを終了します
                peerConnection.Dispose();
                DataChannels.Remove(chatChannel);

                // ホストが切断したときのイベントを発火
                if (state != RTCPeerConnectionState.closed)
                {
                    // TODO: 自身のDisposeを除いた切断判定が来たら
                    OnHostClosed(state);
                }
            }
        };

        return peerConnection;
    }

    /// <summary>
    /// ホストクライアント
    /// </summary>
    public class Host : DrawNetworkClient
    {
        /// <summary>
        /// ルームの待ち受けを行うシグナリングホスト (起動中は接続数を1消費します)
        /// </summary>
        public WebRTCFirebaseSignaling.SignalingHost? RoomHost { get; private set; }

        public override bool IsSignaling => RoomHost != null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="routingInfo">接続情報</param>
        /// <param name="auth">認証情報</param>
        public Host(DrawNetworkRoutingInfo routingInfo, DrawNetworkAuth auth)
            : base(routingInfo, auth)
        {
        }

        // ファイナライズ
        public override void Dispose()
        {
            // 終了時のイベントを発火
            base.OnDispose();
            // シグナリングを終了
            RoomHost?.Dispose();
            RoomHost = null;

            // すべてのピアを切断
            PeerConnections.ForEach(peer => peer.Dispose());
            PeerConnections.Clear();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// ルームの待ち受けを開始
        /// </summary>
        public override async Task StartSignaling()
        {
            if (RoomHost != null)
            {
                Logger.Info($"Skipping start invite as already started.");
                return;
            }

            // シグナリングを開始
            Logger.Info($"Starting invite with Client ID '{Auth.ClientId}'...");
            RoomHost = await Connector.StartAsHost();
        }

        /// <summary>
        /// ルームの待ち受けを停止
        /// </summary>
        public override void StopSignaling()
        {
            if (RoomHost == null)
            {
                Logger.Info($"Skipping stop invite as already stopped.");
                return;
            }

            // シグナリングを停止
            Logger.Info($"Stopping invite...");
            RoomHost?.Dispose();
            RoomHost = null;
        }
    }

    /// <summary>
    /// ゲストクライアント
    /// </summary>
    public class Guest : DrawNetworkClient
    {
        /// <summary>
        /// ルームID
        /// </summary>
        public string RoomId { get; }

        /// <summary>
        /// 参加用のシグナリングを行うピア
        /// </summary>
        public WebRTCFirebaseSignaling.SignalingPeer? Signaler { get; private set; }

        public override bool IsSignaling => Signaler != null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="routingInfo">接続情報</param>
        /// <param name="auth">認証情報</param>
        /// <param name="roomId">ルームID</param>
        public Guest(DrawNetworkRoutingInfo routingInfo, DrawNetworkAuth auth, string roomId)
            : base(routingInfo, auth)
        {
            RoomId = roomId;
        }

        // ファイナライズ
        public override void Dispose()
        {
            // 終了時のイベントを発火
            base.OnDispose();

            // 終了時にシグナリングを行っていれば終了する
            Signaler?.Dispose();
            Signaler = null;
            // すべてのピアを切断
            PeerConnections.ForEach(peer => peer.Dispose());
            PeerConnections.Clear();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// ルームに参加
        /// </summary>
        public override async Task StartSignaling()
        {
            // シグナリングを開始
            Logger.Info($"Starting guest with Client ID '{Auth.ClientId}' and Room ID '{RoomId}'...");
            Signaler = await Connector.StartAsGuest(RoomId);

            // 接続時に完了
            OnConnected += () =>
            {
                // ゲストは接続したらシグナリングを終了
                Signaler?.Dispose();
                Signaler = null;
            };
        }

        /// <summary>
        /// シグナリング中の場合は中止、既に接続中の場合はなにもしない
        /// </summary>
        public override void StopSignaling()
        {
            // シグナリングを停止
            if (Signaler != null)
            {
                Logger.Info($"Canceling to join room...");
            }
            Signaler?.Dispose();
            Signaler = null;
        }
    }
}
