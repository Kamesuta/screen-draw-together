using ScreenDrawTogether.Core;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// WebRTCSyncInkCanvas.xaml の相互作用ロジック
    /// </summary>
    public partial class WebRTCSyncInkCanvas : Window
    {
        public DrawNetworkAuth Auth { get; private set; }
        public DrawNetworkClient Client { get; private set; }
        public Action<string> OurIdCallback { get; }

        public WebRTCSyncInkCanvas(string presetId, bool isHost, string? roomId, Action<string> ourIdCallback)
        {
            InitializeComponent();

            OurIdCallback = ourIdCallback;

            InkCanvas.CanvasStylusDown += StylusPlugin_StylusDown;
            InkCanvas.CanvasStylusMove += StylusPlugin_StylusMove;
            InkCanvas.CanvasStylusUp += StylusPlugin_StylusUp;

            Title += $" - {(isHost ? "Host" : "Guest")} (Preset: {presetId})";

            var routingInfo = DrawNetworkRoutingInfo.Default;
            async void Setup()
            {
                Auth = await DrawNetworkAuth.Login(routingInfo, presetId);
                OurIdCallback(Auth.ClientId);
                Client = isHost
                    ? await DrawNetworkClient.StartAsHost(routingInfo, Auth)
                    : await DrawNetworkClient.StartAsGuest(routingInfo, Auth, roomId);

                Client.OnMessage += (RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data) =>
                {
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
            }
            Setup();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Client.Dispose();
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
            Client.DataChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
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
                Client.DataChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
            }
        }

        private void StylusPlugin_StylusUp(RawStylusInput e)
        {
            using var memoryStream = new MemoryStream();
            memoryStream.WriteByte((byte)StrokePacketType.StrokeDown);

            // 送信
            Client.DataChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
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
