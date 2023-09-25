using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ZXing.QrCode.Internal;
using ZXing;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using ZXing.Windows.Compatibility;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenDrawTogether.Common;

/// <summary>
/// QRコード描画/読み取り
/// </summary>
public class DrawQR
{
    /// <summary>
    /// QRコードの位置
    /// </summary>
    private enum QRPosition
    {
        // 左上
        LeftTop = 0,
        // 右下
        RightBottom = 1,
    }

    /// <summary>
    /// QRコードに埋め込む情報
    /// </summary>
    private struct QRInfo
    {
        [JsonPropertyName("pos")]
        public QRPosition Position { get; set; }

        [JsonPropertyName("room")]
        public string RoomId { get; set; }
    }

    /// <summary>
    /// ルームを共有するためのQRコードを作成
    /// TODO: ルームのオーナー名などを埋め込む
    /// TODO: 接続経路のURLを埋め込む
    /// </summary>
    /// <param name="roomId"></param>
    /// <returns>左上にQRコード、右上にQRコードの読み取り結果を表示したBitmap</returns>
    public static (Bitmap, Bitmap) CreateShareRoomQR(string roomId)
    {
        // QRコードに埋め込む情報を作成
        var leftTopInfo = JsonSerializer.Serialize(new QRInfo
        {
            Position = QRPosition.LeftTop,
            RoomId = roomId,
        });
        var rightBottomInfo = JsonSerializer.Serialize(new QRInfo
        {
            Position = QRPosition.RightBottom,
            RoomId = roomId,
        });
        // QRコードを作成
        Bitmap leftTopQR = CreateQR(leftTopInfo, Properties.Resource.QRLeftTop);
        Bitmap rightBottomQR = CreateQR(rightBottomInfo, Properties.Resource.QRRightBottom);
        return (leftTopQR, rightBottomQR);
    }

    /// <summary>
    /// QRコードを作成
    /// </summary>
    /// <param name="text">埋め込むテキスト</param>
    /// <param name="logo">ロゴ</param>
    /// <returns>QRコード</returns>
    private static Bitmap CreateQR(string text, Bitmap logo)
    {
        // バーコード作成設定
        BarcodeWriter writer = new()
        {
            // バーコードの種類をQRコードに設定
            Format = BarcodeFormat.QR_CODE,
            Options =
                {
                    Width = 400,
                    Height = 400,
                    Margin = 2,
                    Hints =
                    {
                        // QRコードの誤り訂正レベルを最高に設定
                        { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.H },
                        // QRコードの文字コードをUTF-8に設定
                        { EncodeHintType.CHARACTER_SET, "UTF-8" },
                    },
                },
        };

        // QRコードを作成
        var bmp = writer.WriteAsBitmap(text);

        // Bitmapの中心にロゴを描画
        using (var graphics = Graphics.FromImage(bmp))
        {
            var logoSize = new Size(bmp.Width / 5, bmp.Height / 5);
            var logoBackSize = new Size(bmp.Width / 4, bmp.Height / 4);
            graphics.FillRectangle(Brushes.White, new Rectangle(new Point((bmp.Width - logoBackSize.Width) / 2, (bmp.Height - logoBackSize.Height) / 2), logoBackSize));
            graphics.DrawImage(logo, new Rectangle(new Point((bmp.Width - logoSize.Width) / 2, (bmp.Height - logoSize.Height) / 2), logoSize));
        }

        return bmp;
    }
}
