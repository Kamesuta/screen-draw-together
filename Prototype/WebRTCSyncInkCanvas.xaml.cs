using Firebase.Auth;
using Firebase.Auth.Repository;
using FireSharp.Core.Config;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WebRTCFirebaseSignaling;

namespace screen_draw_together.Prototype
{
    /// <summary>
    /// WebRTCSyncInkCanvas.xaml の相互作用ロジック
    /// </summary>
    public partial class WebRTCSyncInkCanvas : Window
    {
        public string PresetId { get; }
        public bool IsHost { get; }
        public string? RoomId { get; }
        public Action<string> OurIdCallback { get; }

        public WebRTCSyncInkCanvas(string presetId, bool isHost, string? roomId, Action<string> ourIdCallback)
        {
            InitializeComponent();

            PresetId = presetId;
            IsHost = isHost;
            RoomId = roomId;
            OurIdCallback = ourIdCallback;

            InkCanvas.syncPlugin.StylusDown += StylusPlugin_StylusDown;
            InkCanvas.syncPlugin.StylusMove += StylusPlugin_StylusMove;
            InkCanvas.syncPlugin.StylusUp += StylusPlugin_StylusUp;

            Title += $" - {(IsHost ? "Host" : "Guest")} (Preset: {PresetId})";

            async void Setup()
            {
                await Login();
                await Connect();
            }
            Setup();
        }

        public string ClientId { get; private set; }
        public string ClientIdToken { get; private set; }

        private async Task Login()
        {
            // FirebaseプロジェクトのAPIキーを使用してFirebase認証コンフィグを作成
            Debug.WriteLine("Start authention with Firebase.");
            var authConfig = new FirebaseAuthConfig
            {
                ApiKey = "AIzaSyAOpybQOeDJt17_yVYSaTzx-ZH_h9y5zL8",
                AuthDomain = "screen-draw-together.firebaseapp.com",
                UserRepository = new FileUserRepository($"FirebaseWebRtcSignaling/{PresetId}"), // 永続ログインデータ: %AppData%\FirebaseWebRtcSignaling
            };
            // FirebaseAuthClientを作成
            var authClient = new FirebaseAuthClient(authConfig);

            // ログイン
            User user;
            if (authClient.User != null)
            {
                Debug.WriteLine("Use existing login.");
                user = authClient.User;
            }
            else
            {
                Debug.WriteLine("Signing in anonymously.");
                var cred = await authClient.SignInAnonymouslyAsync();
                user = cred.User;
            }
            ClientId = user.Uid;
            ClientIdToken = await user.GetIdTokenAsync();
            Debug.WriteLine($"Client ID is {ClientId}");

            OurIdCallback(ClientId);
        }

        public WebRTCFirebaseSignalingConnector Connector { get; private set; }
        public List<RTCDataChannel> ChatChannels { get; private set; } = new();
        private IDisposable _connection;

        private async Task Connect()
        {
            // Initialize the connection with a STUN server to allow remote access
            var config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer>()
                {
                    // Metred Open Relay STUN/TURN server
                    new RTCIceServer()
                    {
                        urls = "stun:stun.relay.metered.ca:80," +
                            "turn:a.relay.metered.ca:80," +
                            "turn:a.relay.metered.ca:80?transport=tcp," +
                            "turn:a.relay.metered.ca:443," +
                            "turn:a.relay.metered.ca:443?transport=tcp",
                        username = "dbdc990c4e887b45b0b264d8",
                        credential = "2U/sMrrs+RSxLYT8"
                    },
                }
            };

            async Task<RTCPeerConnection> CreatePeerConnection()
            {
                // Create a new peer connection automatically disposed at the end of the program
                var peerConnection = new RTCPeerConnection(config);

                var dataChannelLabel = "data_channel";
                Debug.WriteLine($"Adding data channel with label '{dataChannelLabel}'");
                var chatChannelOption = new RTCDataChannelInit()
                {
                    ordered = true,
                    negotiated = true,
                    id = 0,
                };
                var chatChannel = await peerConnection.createDataChannel(dataChannelLabel, chatChannelOption);
                ChatChannels.Add(chatChannel);

                chatChannel.onmessage += (RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data) =>
                {
                    foreach (var channel in ChatChannels)
                    {
                        // ホストは通信を他のピアにリレーする
                        if (channel != dc)
                        {
                            channel.send(data);
                        }
                    }

                    using var stream = new BinaryReader(new MemoryStream(data));
                    StrokePacketType packetType = (StrokePacketType)stream.ReadByte();
                    switch (packetType)
                    {
                        case StrokePacketType.StrokeDown:
                        case StrokePacketType.StrokeUp:
                            {
                                EndStroke();
                            }
                            break;

                        case StrokePacketType.StrokeMove:
                            {
                                double x = stream.ReadDouble();
                                double y = stream.ReadDouble();
                                AddStrokePoint(new StylusPoint(x, y));
                            }
                            break;
                    }
                };

                return peerConnection;
            }


            // Firebase config
            var firebaseConfig = new FirebaseConfig()
            {
                BasePath = "https://screen-draw-together-default-rtdb.asia-southeast1.firebasedatabase.app",
                AuthSecret = ClientIdToken,
            };

            //using var signaler = new NamedPipeSignaler.NamedPipeSignaler(pc, "testpipe");
            Connector = new WebRTCFirebaseSignaling.WebRTCFirebaseSignalingConnector(ClientId)
            {
                FirebaseConfig = firebaseConfig,
                CreatePeerConnection = CreatePeerConnection,
            };

            // Setup signaling
            if (IsHost)
            {
                Debug.WriteLine($"Starting host with Client ID '{ClientId}'...");
                var host = await Connector.StartAsHost();
                _connection = host;

                // とりあえず今回はHostが終了したら全ピアを解放
                host.OnDispose += () =>
                {
                    host.PeerList.ForEach((peer) => peer.Dispose());
                    host.PeerList.Clear();
                };
            }
            else
            {
                Debug.WriteLine($"Starting guest with Client ID '{ClientId}' and Room ID '{RoomId}'...");
                var signaler = await Connector.StartAsGuest(RoomId);
                _connection = signaler;
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _connection?.Dispose();
        }

        public enum StrokePacketType
        {
            None,
            SyncInfo,
            StrokeDown,
            StrokeMove,
            StrokeUp,
        }

        private Stroke? stroke;

        private void StylusPlugin_StylusDown(RawStylusInput e)
        {
            using var memoryStream = new MemoryStream();
            memoryStream.WriteByte((byte)StrokePacketType.StrokeDown);

            // 送信
            ChatChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
        }

        private void StylusPlugin_StylusMove(RawStylusInput e)
        {
            StylusPointCollection points = e.GetStylusPoints();

            // すべての入力ポイントを送信
            foreach (var point in points)
            {
                using var memoryStream = new MemoryStream();
                using var writer = new BinaryWriter(memoryStream);
                writer.Write((byte)StrokePacketType.StrokeMove);
                writer.Write(point.X);
                writer.Write(point.Y);

                // 送信
                ChatChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
            }
        }

        private void StylusPlugin_StylusUp(RawStylusInput e)
        {
            using var memoryStream = new MemoryStream();
            memoryStream.WriteByte((byte)StrokePacketType.StrokeDown);

            // 送信
            ChatChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
        }

        private void AddStrokePoint(StylusPoint point)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (stroke == null)
                {
                    stroke = new(new(new List<StylusPoint>() { new StylusPoint(point.X, point.Y) }))
                    {
                        DrawingAttributes = InkCanvas.DefaultDrawingAttributes
                    };

                    InkCanvas.Strokes.Add(stroke);
                }
                else
                {
                    stroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
                }
            }));
        }

        private void EndStroke()
        {
            stroke = null;
        }
    }
}
