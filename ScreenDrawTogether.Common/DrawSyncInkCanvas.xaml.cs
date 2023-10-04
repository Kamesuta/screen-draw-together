using ScreenDrawTogether.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Threading;
using static ScreenDrawTogether.Core.DrawNetworkClient;

namespace ScreenDrawTogether.Common;

/// <summary>
/// DrawSyncInkCanvas.xaml の相互作用ロジック
/// </summary>
public partial class DrawSyncInkCanvas : Window
{
    /// <summary>
    /// クライアント
    /// </summary>
    public DrawNetworkClient Client { get; private set; }

    // ストローク
    private Stroke? _stroke;

    // タイマー
    private DispatcherTimer? _timer;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="client">クライアント</param>
    public DrawSyncInkCanvas(DrawNetworkClient client)
    {
        InitializeComponent();

        Client = client;

        // 他人の入力: メッセージ受信時のイベントを登録
        Client.OnStrokeOff += StrokeOff;
        Client.OnStrokeAddPoints += StrokeAddPoints;

        // 自分の入力: ストローク開始/終了/移動時のイベントを登録
        InkCanvas.CanvasStylusDown += (e) => Client.SendStrokeOff();
        InkCanvas.CanvasStylusUp += (e) => Client.SendStrokeOff();
        InkCanvas.CanvasStylusMove += (e) => Client.SendStrokeAddPoints(e.GetStylusPoints().Select(point => new DrawPoint { X = point.X, Y = point.Y }));

        // 定期的にパケットを送信
        _timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        _timer.Tick += (sender, e) => Client.SendPacket();
        _timer.Start();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // タイマーを停止
        _timer?.Stop();
        _timer = null;

        // 他人の入力: ウィンドウを閉じるときにメッセージ受信時のイベントを解除
        Client.OnStrokeOff -= StrokeOff;
        Client.OnStrokeAddPoints -= StrokeAddPoints;
    }

    /// <summary>
    /// 招待用のQRコードを表示
    /// </summary>
    /// <param name="roomId">ルームID</param>
    public void SetInviteRoomId(string? roomId)
    {
        if (roomId != null)
        {
            // QR生成
            (var leftTop, var rightBottom) = DrawQR.CreateShareRoomQR(roomId);
            // QRコードを表示
            QRLeftTop.Source = leftTop.ToImageSource();
            QRRightBottom.Source = rightBottom.ToImageSource();
        }
        else
        {
            QRLeftTop.Source = null;
            QRRightBottom.Source = null;
        }
    }

    /// <summary>
    /// 他人の入力: ストローク終了時
    /// </summary>
    private void StrokeOff()
    {
        // ストロークを切る
        _stroke = null;
    }

    /// <summary>
    /// 他人の入力: ストロークを追加
    /// </summary>
    /// <param name="points">点</param>
    private void StrokeAddPoints(IEnumerable<DrawPoint> points)
    {
        // UIスレッドで実行
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // ストロークにポイントを追加
            foreach (var point in points)
            {
                // ストロークがなければ作成
                if (_stroke == null)
                {
                    // 新しいストロークを作成
                    _stroke = new(new(new List<StylusPoint>() { new StylusPoint(point.X, point.Y) }))
                    {
                        // デフォルトの描画属性を使用
                        DrawingAttributes = InkCanvas.DefaultDrawingAttributes
                    };

                    // ストロークを追加
                    InkCanvas.Strokes.Add(_stroke);
                }
                else
                {
                    _stroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
                }
            }
        }));
    }
}
