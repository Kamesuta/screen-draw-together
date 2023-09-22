using ScreenDrawTogether.Common;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Point = System.Drawing.Point;

namespace ScreenDrawTogether.Pages
{
    /// <summary>
    /// SelectScreen.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectScreen : Page
    {
        private SelectBorder? _overlayWindow;
        private ImageSource? _confirmedPreview;

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

        private static ImageSource GetPreview(Rect rect)
        {
            var captureBmp = new Bitmap((int)rect.Width, (int)rect.Height);
            using (var captureGraphics = Graphics.FromImage(captureBmp))
            {
                captureGraphics.CopyFromScreen(new Point((int)rect.Left, (int)rect.Top), new Point(0, 0), captureBmp.Size);
            }
            return captureBmp.ToImageSource();
        }

        private void SelectBorder_OnRectConfirmed(Rect rect)
        {
            _overlayWindow?.Close();

            if (rect == Rect.Empty)
            {
                Preview.Source = null;
            }
            else
            {
                Preview.Source = _confirmedPreview = GetPreview(rect);
            }
        }

        private void SelectBorder_OnRectUpdated(Rect rect)
        {
            if (rect == Rect.Empty)
            {
                Preview.Source = _confirmedPreview;
            }
            else
            {
                Preview.Source = GetPreview(rect);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _overlayWindow?.Close();
        }
    }
}
