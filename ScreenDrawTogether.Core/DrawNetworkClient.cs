using FireSharp.Core.Config;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreenDrawTogether.Core;

/// <summary>
/// ネットワーク接続
/// </summary>
public class DrawNetworkClient : IDisposable
{
    /// <summary>
    /// ネットワーク用ロガー
    /// </summary>
    private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Network");

    /// <summary>
    /// ホストかどうか
    /// </summary>
    public bool IsHost { get; }

    /// <summary>
    /// ルームID
    /// </summary>
    public string RoomId { get; }

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
    public WebRTCFirebaseSignaling.SignalingConnector? Connector { get; private set; }

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
    /// <param name="isHost">ホストかどうか</param>
    /// <param name="roomId">ルームID</param>
    private DrawNetworkClient(DrawNetworkRoutingInfo routingInfo, DrawNetworkAuth auth, bool isHost, string roomId)
    {
        RoutingInfo = routingInfo;
        Auth = auth;
        IsHost = isHost;
        RoomId = roomId;
    }

    // ファイナライズ
    public void Dispose()
    {
        OnDispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// ネットワークに接続する
    /// </summary>
    /// <param name="routingInfo">接続情報</param>
    /// <param name="auth">認証情報</param>
    /// <returns>ネットワーク接続</returns>
    public static async Task<DrawNetworkClient> StartAsHost(DrawNetworkRoutingInfo routingInfo, DrawNetworkAuth auth)
    {
        // ネットワーク
        var network = new DrawNetworkClient(routingInfo, auth, true, auth.ClientId);
        // シグナリングコネクターを作成
        var connector = network.CreateSignaler();

        // シグナリングを開始
        Logger.Info($"Starting host with Client ID '{auth.ClientId}'...");
        var host = await connector.StartAsHost();

        // 終了時に完了
        network.OnDispose += () =>
        {
            host.Dispose();

            // すべてのピアを切断
            network.PeerConnections.ForEach(peer => peer.Dispose());
        };

        return network;
    }

    /// <summary>
    /// ネットワークに接続する
    /// </summary>
    /// <param name="routingInfo">接続情報</param>
    /// <param name="auth">認証情報</param>
    /// <param name="roomId">ルームID</param>
    /// <returns>ネットワーク接続</returns>
    public static async Task<DrawNetworkClient> StartAsGuest(DrawNetworkRoutingInfo routingInfo, DrawNetworkAuth auth, string roomId)
    {
        // ネットワーク
        var network = new DrawNetworkClient(routingInfo, auth, false, roomId);
        // シグナリングコネクターを作成
        var connector = network.CreateSignaler();

        // シグナリングを開始
        Logger.Info($"Starting guest with Client ID '{auth.ClientId}' and Room ID '{roomId}'...");
        var signaler = await connector.StartAsGuest(roomId);

        // 終了時に完了
        network.OnDispose += () =>
        {
            signaler.PeerConnection.Dispose();
            signaler.Dispose();
        };
        // 接続時に完了
        network.OnConnected += () =>
        {
            // ゲストは接続したらシグナリングを終了
            signaler.Dispose();
        };

        return network;
    }

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
                    OnHostClosed(state);
                }
            }
        };

        return peerConnection;
    }
}
