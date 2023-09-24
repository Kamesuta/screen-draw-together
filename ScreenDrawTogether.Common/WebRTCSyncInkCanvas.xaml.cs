using ScreenDrawTogether.Core;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;

namespace ScreenDrawTogether.Common;

/// <summary>
/// WebRTCSyncInkCanvas.xaml の相互作用ロジック
/// </summary>
public partial class WebRTCSyncInkCanvas : Window
{
    /// <summary>
    /// クライアント
    /// </summary>
    public DrawNetworkClient Client { get; private set; }

    /// <summary>
    /// ストローク
    /// </summary>
    private Stroke? stroke;

    /// <summary>
    /// ストロークパケットタイプ
    /// </summary>
    public enum StrokePacketType
    {
        /// <summary>
        /// パケットなし
        /// </summary>
        None,
        /// <summary>
        /// 同期情報
        /// </summary>
        SyncInfo,
        /// <summary>
        /// ストローク開始
        /// </summary>
        StrokeDown,
        /// <summary>
        /// ストローク移動
        /// </summary>
        StrokeMove,
        /// <summary>
        /// ストローク終了
        /// </summary>
        StrokeUp,
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="routingInfo">接続情報</param>
    /// <param name="auth">認証情報</param>
    /// <param name="roomId">ルームID</param>
    public WebRTCSyncInkCanvas(DrawNetworkClient client)
    {
        InitializeComponent();

        Client = client;

        // 他人の入力: メッセージ受信時のイベントを登録
        Client.OnMessage += OnMessage;

        // 自分の入力: ストローク開始/終了/移動時のイベントを登録
        InkCanvas.CanvasStylusDown += StylusPlugin_StylusDown;
        InkCanvas.CanvasStylusMove += StylusPlugin_StylusMove;
        InkCanvas.CanvasStylusUp += StylusPlugin_StylusUp;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 他人の入力: ウィンドウを閉じるときにメッセージ受信時のイベントを解除
        Client.OnMessage -= OnMessage;
    }

    /// <summary>
    /// 自分の入力: ストローク開始時
    /// </summary>
    /// <param name="e">入力</param>
    private void StylusPlugin_StylusDown(RawStylusInput e)
    {
        // バイナリに変換
        using var memoryStream = new MemoryStream();
        memoryStream.WriteByte((byte)StrokePacketType.StrokeDown);

        // 送信
        Client?.DataChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
    }

    /// <summary>
    /// 自分の入力: ストローク動く
    /// </summary>
    /// <param name="e">入力</param>
    private void StylusPlugin_StylusMove(RawStylusInput e)
    {
        StylusPointCollection points = e.GetStylusPoints();

        // すべての入力ポイントを送信
        foreach (var point in points)
        {
            // バイナリに変換
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.Write((byte)StrokePacketType.StrokeMove);
            writer.Write(point.X);
            writer.Write(point.Y);

            // 送信
            Client?.DataChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
        }
    }

    /// <summary>
    /// 自分の入力: ストローク終了時
    /// </summary>
    /// <param name="e">入力</param>
    private void StylusPlugin_StylusUp(RawStylusInput e)
    {
        // バイナリに変換
        using var memoryStream = new MemoryStream();
        memoryStream.WriteByte((byte)StrokePacketType.StrokeDown);

        // 送信
        Client?.DataChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
    }

    /// <summary>
    /// 他人の入力: メッセージ受信時
    /// </summary>
    /// <param name="dc">データのチャネル</param>
    /// <param name="protocol">プロトコル</param>
    /// <param name="data">データ</param>
    private void OnMessage(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data)
    {
        // バイナリからストロークパケットを復元
        using var stream = new BinaryReader(new MemoryStream(data));
        // パケットタイプを読み込み
        StrokePacketType packetType = (StrokePacketType)stream.ReadByte();

        // パケットタイプによって処理を分岐
        switch (packetType)
        {
            // ストローク開始/終了時はストロークを切る
            case StrokePacketType.StrokeDown:
            case StrokePacketType.StrokeUp:
                {
                    EndStroke();
                }
                break;

            // ストローク移動時はポイントを追加
            case StrokePacketType.StrokeMove:
                {
                    // ポイントを読み込み
                    double x = stream.ReadDouble();
                    double y = stream.ReadDouble();
                    // ストロークにポイントを追加
                    AddStrokePoint(new StylusPoint(x, y));
                }
                break;
        }
    }

    /// <summary>
    /// 他人の入力: ストロークを追加
    /// </summary>
    /// <param name="point"></param>
    private void AddStrokePoint(StylusPoint point)
    {
        // UIスレッドで実行
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // ストロークがなければ作成
            if (stroke == null)
            {
                // 新しいストロークを作成
                stroke = new(new(new List<StylusPoint>() { new StylusPoint(point.X, point.Y) }))
                {
                    // デフォルトの描画属性を使用
                    DrawingAttributes = InkCanvas.DefaultDrawingAttributes
                };

                // ストロークを追加
                InkCanvas.Strokes.Add(stroke);
            }
            else
            {
                // ストロークにポイントを追加
                stroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
            }
        }));
    }

    /// <summary>
    /// 他人の入力: ストローク終了時
    /// </summary>
    private void EndStroke()
    {
        // ストロークを切る
        stroke = null;
    }
}
