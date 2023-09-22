using ScreenDrawTogether.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenDrawTogether.Pages
{
    /// <summary>
    /// SelectScreen.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectScreen : Page
    {
        private SelectBorder? _overlayWindow;
        private ImageSource? _confirmedPreview;
        private HWndRect _confirmedRect = HWndRect.Empty;

        public SelectScreen()
        {
            InitializeComponent();
        }

        private void SelectMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            _overlayWindow?.Close();
            _overlayWindow = new SelectBorder()
            {
                Mode = SelectBorder.SelectMode.Monitor,
            };
            _overlayWindow.OnRectConfirmed += SelectBorder_OnRectConfirmed;
            _overlayWindow.Show();
        }

        private void SelectWindowButton_Click(object sender, RoutedEventArgs e)
        {
            _overlayWindow?.Close();
            _overlayWindow = new SelectBorder()
            {
                Mode = SelectBorder.SelectMode.Window,
            };
            _overlayWindow.OnRectConfirmed += SelectBorder_OnRectConfirmed;
            _overlayWindow.OnRectUpdated += SelectBorder_OnRectUpdated;
            _overlayWindow.Show();
        }

        private static ImageSource GetPreview(HWndRect hWndRect)
        {
            return WindowRectUtility.PrintWindow(hWndRect).ToImageSource();
        }

        private void SelectBorder_OnRectConfirmed(HWndRect hWndRect)
        {
            _overlayWindow?.Close();

            _confirmedRect = hWndRect;
            if (hWndRect.Rect == Rect.Empty)
            {
                Preview.Source = null;
                StartButton.IsEnabled = false;
            }
            else
            {
                Preview.Source = _confirmedPreview = GetPreview(hWndRect);
                StartButton.IsEnabled = true;
            }
        }

        private void SelectBorder_OnRectUpdated(HWndRect hWndRect)
        {
            if (hWndRect.Rect == Rect.Empty)
            {
                Preview.Source = _confirmedPreview;
            }
            else
            {
                Preview.Source = GetPreview(hWndRect);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _overlayWindow?.Close();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Host(_confirmedRect));
        }
    }
}
