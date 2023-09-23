using System.Threading.Tasks;
using System.Windows;
using ScreenDrawTogether.Common;
using ScreenDrawTogether.Core;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        private AlphaOverlay? alphaOverlay;

        private SyncInkCanvas? syncInkCanvas;

        private WebRTCSyncInkCanvas? webRtcSyncInkCanvasA;
        private WebRTCSyncInkCanvas? webRtcSyncInkCanvasB;
        private WebRTCSyncInkCanvas? webRtcSyncInkCanvasC;

        private SelectWindow? selectWindow;
        private SelectBorder? selectBorder;
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

        private async Task<WebRTCSyncInkCanvas?> CreateWebRTCSyncInkCanvas(string presetId, WebRTCSyncInkCanvas? canvas)
        {
            // 接続情報
            DrawNetworkRoutingInfo routingInfo = DrawNetworkRoutingInfo.Default;

            // ルームID (nullの場合はホスト)
            string? roomId = RoomIDTextBox.Text.Length == 0 ? null : RoomIDTextBox.Text;

            if (canvas == null)
            {
                // ログイン
                DrawNetworkAuth auth = await DrawNetworkAuth.Login(routingInfo, presetId);
                // ルームIDを表示
                RoomIDTextBox.Text = roomId == null ? auth.ClientId : roomId;

                // キャンバスを作成
                canvas = new WebRTCSyncInkCanvas(routingInfo, auth, roomId);
                // タイトルにプリセットIDを表示
                canvas.Title += $" - {(roomId == null ? "Host" : "Guest")} (Preset: {presetId})";
                canvas.Show();
            }
            else
            {
                canvas.Close();
                canvas = null;
            }

            return canvas;
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
            selectBorder = new SelectBorder()
            {
                Mode = SelectBorder.SelectMode.Window
            };
            selectBorder.Show();
        }

        private void SelectBorderOpenMonitor_Click(object sender, RoutedEventArgs e)
        {
            selectBorder?.Close();
            selectBorder = new SelectBorder()
            {
                Mode = SelectBorder.SelectMode.Monitor
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
