using System.Windows;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// SelectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectWindow : Window
    {
        // オーバーレイ
        private SelectBorder? _overlayWindow;

        public SelectWindow()
        {
            InitializeComponent();
        }

        private void CaptureWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_overlayWindow != null)
            {
                // ウィンドウを閉じる
                _overlayWindow.Close();
                _overlayWindow = null;
                // ステート設定
                Resources["CaptureWindowState"] = "OFF";
            }
            else
            {
                // ウィンドウを作成
                _overlayWindow = new SelectBorder();
                _overlayWindow.OnRectConfirmed += (rect) =>
                {
                    // ウィンドウを閉じる
                    _overlayWindow?.Close();
                    _overlayWindow = null;
                    // ステート設定
                    Resources["CaptureWindowState"] = "OFF";

                    // ウィンドウの範囲を表示
                    if (rect != Rect.Empty)
                    {
                        CaptureWindowText.Content = $"RECT: Left:{rect.Left}, Top:{rect.Top}, Right:{rect.Right}, Bottom:{rect.Bottom}";
                    }
                    else
                    {
                        CaptureWindowText.Content = "Not found";
                    }
                };
                _overlayWindow.Show();
                // ステート設定
                Resources["CaptureWindowState"] = "ON";
            }
        }
    }
}