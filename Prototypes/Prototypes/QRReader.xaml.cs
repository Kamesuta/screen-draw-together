using ScreenDrawTogether.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using ZXing;
using ZXing.Windows.Compatibility;
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
            int width = (int)SystemParameters.VirtualScreenWidth;
            int height = (int)SystemParameters.VirtualScreenHeight;

            // スクリーンキャプチャ
            var bmp = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bmp))
            {
                graphics.CopyFromScreen((int)SystemParameters.VirtualScreenLeft, (int)SystemParameters.VirtualScreenTop, 0, 0, bmp.Size);
            }
            //bmp.Save("screenshot.png");

            //バーコードの読み取り設定
            BarcodeReader reader = new()
            {
                Options =
                {
                    PossibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE },
                    TryHarder = true,
                },
            };

            // detect and decode the barcode inside the bitmap
            var result = reader.Decode(bmp);
            // do something with the result
            if (result != null)
            {
                TypeText.Content = result.BarcodeFormat.ToString();
                ContentText.Content = result.Text;

                if (result.ResultPoints.Length > 0)
                {
                    var point = result.ResultPoints.ElementAt(0);
                    float x1 = point.X;
                    float y1 = point.Y;
                    float x2 = point.X;
                    float y2 = point.Y;

                    foreach (var resultPoint in result.ResultPoints)
                    {
                        x1 = Math.Min(x1, resultPoint.X);
                        y1 = Math.Min(y1, resultPoint.Y);
                        x2 = Math.Max(x2, resultPoint.X);
                        y2 = Math.Max(y2, resultPoint.Y);
                    }

                    var leftTop = new Point((int)(SystemParameters.VirtualScreenLeft + x1), (int)(SystemParameters.VirtualScreenTop + y1));
                    var rightBottom = new Point((int)(SystemParameters.VirtualScreenLeft + x2), (int)(SystemParameters.VirtualScreenTop + y2));
                    var captureBmp = new Bitmap(rightBottom.X - leftTop.X, rightBottom.Y - leftTop.Y);
                    using (var captureGraphics = Graphics.FromImage(captureBmp))
                    {
                        captureGraphics.CopyFromScreen(leftTop, new Point(0, 0), captureBmp.Size);
                    }
                    CaptureImage.Source = captureBmp.ToImageSource();
                }
            }
            else
            {
                TypeText.Content = "null";
                ContentText.Content = "null";
            }
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
