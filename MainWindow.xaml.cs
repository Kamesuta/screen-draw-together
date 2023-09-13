using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace screen_draw_together
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

        public MainWindow()
        {
            InitializeComponent();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
