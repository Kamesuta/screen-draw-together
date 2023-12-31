﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Firebase.Auth;
using FireSharp.Core.Exceptions;
using ScreenDrawTogether.Common;
using ScreenDrawTogether.Core;
using Brushes = System.Windows.Media.Brushes;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        private AlphaOverlay? alphaOverlay;

        private SyncInkCanvas? syncInkCanvas;

        private DrawSyncInkCanvas? webRtcSyncInkCanvasA;
        private DrawSyncInkCanvas? webRtcSyncInkCanvasB;
        private DrawSyncInkCanvas? webRtcSyncInkCanvasC;

        private SelectWindow? selectWindow;
        private DrawSelectBorder? selectBorder;
        private QRReader? qrReader;

        public DebugWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AlphaOverlayOpen_Click(object sender, RoutedEventArgs e)
        {
            alphaOverlay?.Close();
            alphaOverlay = new AlphaOverlay();
            alphaOverlay.Show();
        }

        private void AlphaOverlayClose_Click(object sender, RoutedEventArgs e)
        {
            alphaOverlay?.Close();
            alphaOverlay = null;
        }

        private void AlphaOverlayClickEnable_Click(object sender, RoutedEventArgs e)
        {
            alphaOverlay?.SetClickable(true);
        }

        private void AlphaOverlayClickDisable_Click(object sender, RoutedEventArgs e)
        {
            alphaOverlay?.SetClickable(false);
        }

        private void SyncInkCanvasOpen_Click(object sender, RoutedEventArgs e)
        {
            syncInkCanvas?.Close();
            syncInkCanvas = new SyncInkCanvas();
            syncInkCanvas.Show();
        }

        private void SyncInkCanvasClose_Click(object sender, RoutedEventArgs e)
        {
            syncInkCanvas?.Close();
            syncInkCanvas = null;
        }

        private void SyncInkCanvasSync_Click(object sender, RoutedEventArgs e)
        {
            syncInkCanvas?.Sync();
        }

        private async void WebRTCSyncInkCanvasA_Click(object sender, RoutedEventArgs e)
        {
            webRtcSyncInkCanvasA = await CreateWebRTCSyncInkCanvas("a", webRtcSyncInkCanvasA);
        }

        private async void WebRTCSyncInkCanvasB_Click(object sender, RoutedEventArgs e)
        {
            webRtcSyncInkCanvasB = await CreateWebRTCSyncInkCanvas("b", webRtcSyncInkCanvasB);
        }

        private async void WebRTCSyncInkCanvasC_Click(object sender, RoutedEventArgs e)
        {
            webRtcSyncInkCanvasC = await CreateWebRTCSyncInkCanvas("c", webRtcSyncInkCanvasC);
        }

        private async Task<DrawSyncInkCanvas?> CreateWebRTCSyncInkCanvas(string presetId, DrawSyncInkCanvas? canvas)
        {
            // 接続情報
            DrawNetworkRoutingInfo routingInfo = DrawNetworkRoutingInfo.Default;

            // ルームID (nullの場合はホスト)
            string? roomId = RoomIDTextBox.Text.Length == 0 ? null : RoomIDTextBox.Text;

            if (canvas == null)
            {
                DrawNetworkAuth auth;
                try
                {
                    // ログイン
                    auth = await DrawNetworkAuth.Login(routingInfo, presetId);
                }
                catch (FirebaseAuthException)
                {
                    NotifyError("接続に失敗しました。\nホストの共有が終了している可能性があります。");
                    return null;
                }

                // ルームIDを表示
                RoomIDTextBox.Text = roomId ?? auth.ClientId;

                // ピアを作成
                DrawNetworkPeer peer = roomId == null
                        // ホスト
                        ? new DrawNetworkPeer.Host(routingInfo, auth)
                        // ゲスト
                        : new DrawNetworkPeer.Guest(routingInfo, auth) { RoomId = roomId };

                // シグナリング開始
                try
                {
                    await peer.StartSignaling();
                }
                catch (FirebaseException)
                {
                    NotifyError("接続に失敗しました。\nホストの共有が終了している可能性があります。");
                    return null;
                }

                // 切断時
                peer.OnHostClosed += (state) =>
                {
                    // UIスレッドで実行
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        NotifyError("通信が切断されました");
                        // 切断されたらキャンバスを閉じる
                        canvas?.Close();
                    }));
                };
                // 接続時
                peer.OnConnected += () =>
                {
                    // UIスレッドで実行
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // 接続したらタイトルに接続済みを表示
                        if (canvas != null)
                        {
                            canvas.Title += " - Connected";
                        }
                    }));
                };

                // クライアント作成
                DrawNetworkClient client = new(peer);

                // キャンバスを作成
                canvas = new DrawSyncInkCanvas(client)
                {
                    Background = Brushes.White,
                    WindowStyle = WindowStyle.SingleBorderWindow,
                    AllowsTransparency = false,
                };
                // タイトルにプリセットIDを表示
                canvas.Title += $" - {(roomId == null ? "Host" : "Guest")} (Preset: {presetId})";
                canvas.Show();
                // キャンバス終了時にピアを切断
                canvas.Closed += (sender, e) =>
                {
                    client.Dispose();
                    peer.Dispose();
                };
            }
            else
            {
                canvas.Close();
                canvas = null;
            }

            return canvas;
        }

        /// <summary>
        /// エラーを通知
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        private static void NotifyError(string message)
        {
            MessageBox.Show(message, "Screen Draw Together", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SelectWindowOpen_Click(object sender, RoutedEventArgs e)
        {
            selectWindow?.Close();
            selectWindow = new SelectWindow();
            selectWindow.Show();
        }

        private void SelectWindowClose_Click(object sender, RoutedEventArgs e)
        {
            selectWindow?.Close();
            selectWindow = null;
        }

        private void SelectBorderOpenWindow_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.Close();
            selectBorder = new DrawSelectBorder()
            {
                Mode = DrawSelectBorder.SelectMode.Window
            };
            selectBorder.Show();
        }

        private void SelectBorderOpenMonitor_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.Close();
            selectBorder = new DrawSelectBorder()
            {
                Mode = DrawSelectBorder.SelectMode.Monitor
            };
            selectBorder.Show();
        }

        private void SelectBorderClose_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.Close();
            selectBorder = null;
        }

        private void QRReaderOpen_Click(object sender, RoutedEventArgs e)
        {
            qrReader?.Close();
            qrReader = new QRReader();
            qrReader.Show();

        }

        private void QRReaderClose_Click(object sender, RoutedEventArgs e)
        {
            qrReader?.Close();
            qrReader = null;
        }
    }
}
