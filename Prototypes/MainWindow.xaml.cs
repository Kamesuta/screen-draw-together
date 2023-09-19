﻿using System.Windows;
using ScreenDrawTogether.Common;

namespace ScreenDrawTogether
{
    using Prototype;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AlphaOverlay? alphaOverlay;

        private SyncInkCanvas? syncInkCanvas;

        private WebRTCSyncInkCanvas? webRtcSyncInkCanvasA;
        private WebRTCSyncInkCanvas? webRtcSyncInkCanvasB;
        private WebRTCSyncInkCanvas? webRtcSyncInkCanvasC;

        private SelectWindow? selectWindow;
        private SelectBorder? selectBorder;
        private QRReader? qrReader;

        public MainWindow()
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

        private void WebRTCSyncInkCanvasA_Click(object sender, RoutedEventArgs e)
        {
            CreateWebRTCSyncInkCanvas("a", ref webRtcSyncInkCanvasA);
        }

        private void WebRTCSyncInkCanvasB_Click(object sender, RoutedEventArgs e)
        {
            CreateWebRTCSyncInkCanvas("b", ref webRtcSyncInkCanvasB);
        }

        private void WebRTCSyncInkCanvasC_Click(object sender, RoutedEventArgs e)
        {
            CreateWebRTCSyncInkCanvas("c", ref webRtcSyncInkCanvasC);
        }

        private void CreateWebRTCSyncInkCanvas(string presetId, ref WebRTCSyncInkCanvas? canvas)
        {
            string? roomId = RoomIDTextBox.Text.Length == 0 ? null : RoomIDTextBox.Text;
            bool isHost = roomId == null;
            void onRoomIdChanged(string newRoomId)
            {
                if (isHost)
                {
                    RoomIDTextBox.Text = newRoomId;
                }
            }

            if (canvas == null)
            {
                canvas = new WebRTCSyncInkCanvas(presetId, isHost, roomId, onRoomIdChanged);
                canvas.Show();
            }
            else
            {
                canvas.Close();
                canvas = null;
            }
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

        private void SelectBorderOpen_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.Close();
            selectBorder = new SelectBorder();
            selectBorder.Show();
        }

        private void SelectBorderClose_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.Close();
            selectBorder = null;
        }

        private void SelectBorderRect1_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.SetRect(new Rect(100, 100, 200, 200));
        }

        private void SelectBorderRect2_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.SetRect(new Rect(300, 100, 200, 300));
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
