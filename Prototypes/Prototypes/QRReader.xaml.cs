using ScreenDrawTogether.Common;
using System.Drawing;
using System.Windows;
using Point = System.Drawing.Point;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// QRReader.xaml の相互作用ロジック
    /// </summary>
    public partial class QRReader : Window
    {
        public QRReader()
        {
            InitializeComponent();
        }

        /// <summary>
        /// スクショ&QRコード読み取り
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <see cref="https://resanaplaza.com/2022/12/24/%E3%80%90%E3%82%B3%E3%83%94%E3%83%9A"/>
        private void QrReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!DrawQR.TryReadShareRoomQR(out var rect, out var roomId))
            {
                ContentText.Content = "QRコードが見つかりませんでした";
                CaptureImage.Source = null;
                return;
            }

            // ルームIDを表示
            ContentText.Content = roomId;

            // スクショを表示
            var captureBmp = new Bitmap((int)rect.Width, (int)rect.Height);
            using (var captureGraphics = Graphics.FromImage(captureBmp))
            {
                captureGraphics.CopyFromScreen(new Point((int)rect.X, (int)rect.Y), new Point(0, 0), captureBmp.Size);
            }
            CaptureImage.Source = captureBmp.ToImageSource();
        }

        private void QrWriteButton_Click(object sender, RoutedEventArgs e)
        {
            // QRコードを作成
            (var leftTop, var rightBottom) = DrawQR.CreateShareRoomQR(PrintText.Text);

            // QRコードを表示
            QRLeftTop.Source = leftTop.ToImageSource();
            QRRightBottom.Source = rightBottom.ToImageSource();
        }
    }
}
