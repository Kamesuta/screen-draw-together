using ScreenDrawTogether.Core;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
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

    /// <summary>
    /// ストローク
    /// </summary>
    private Stroke? stroke;

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
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
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
        stroke = null;
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
                    stroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
                }
            }
        }));
    }
}
