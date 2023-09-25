using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using ZXing;
using ZXing.QrCode.Internal;
using ZXing.Windows.Compatibility;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using WPoint = System.Windows.Point;

namespace ScreenDrawTogether.Common;

/// <summary>
/// QRコード描画/読み取り
/// </summary>
public class DrawQR
{
    /// <summary>
    /// QRコードの種類
    /// </summary>
    public static readonly string TypeConstant = "screendraw";

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
        /// <summary>
        /// チェック用のQRコードの種類 (TypeConstantと同じ)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// QRの位置
        /// </summary>
        [JsonPropertyName("pos")]
        public QRPosition PositionType { get; set; }

        /// <summary>
        /// ルームID
        /// </summary>
        [JsonPropertyName("room")]
        public string RoomId { get; set; }
    }

    /// <summary>
    /// QRコードを読み取ったときのデータ
    /// </summary>
    private struct QRData
    {
        public string RoomId { get; set; }
        public QRPosition PositionType { get; set; }
        public Point Position { get; set; }
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
            Type = TypeConstant,
            PositionType = QRPosition.LeftTop,
            RoomId = roomId,
        });
        var rightBottomInfo = JsonSerializer.Serialize(new QRInfo
        {
            Type = TypeConstant,
            PositionType = QRPosition.RightBottom,
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

    /// <summary>
    /// スクリーンショットからルームIDと矩形を読み取る
    /// </summary>
    /// <param name="rect">矩形</param>
    /// <param name="roomId">ルームID</param>
    /// <returns>成功したか</returns>
    public static bool TryReadShareRoomQR(out Rect rect, out string roomId)
    {
        // 初期化
        rect = Rect.Empty;
        roomId = "";

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

        // スクリーンショットからQRコードを検出する
        var results = reader.DecodeMultiple(bmp);
        // なにも見つからない場合nullを返してくるので注意
        // QRコードは左上と右下の2つが検出されるはず
        if (results == null || results.Length < 2)
        {
            // 検出なし
            return false;
        }

        // すべてのQRコードをチェック
        List<QRData> qrList = new();
        foreach (var result in results)
        {
            // QRコードの情報を取得
            QRInfo qrInfo;
            try
            {
                qrInfo = JsonSerializer.Deserialize<QRInfo>(result.Text);
            }
            catch (JsonException)
            {
                continue;
            }
            // QRコードの種類が違う
            if (qrInfo.Type != TypeConstant) continue;
            // ルームIDが空 または QRコードの位置が空
            if (qrInfo.RoomId.Length == 0 || result.ResultPoints.Length == 0) continue;

            // QRコードの位置を計算
            Point? position = qrInfo.PositionType switch
            {
                QRPosition.LeftTop => new Point((int)result.ResultPoints.Min(p => p.X), (int)result.ResultPoints.Min(p => p.Y)),
                QRPosition.RightBottom => new Point((int)result.ResultPoints.Max(p => p.X), (int)result.ResultPoints.Max(p => p.Y)),
                _ => null,
            };
            if (position == null) continue;

            // QRコードの情報をリストに追加
            var qr = new QRData()
            {
                RoomId = qrInfo.RoomId,
                PositionType = qrInfo.PositionType,
                Position = position.Value,
            };
            qrList.Add(qr);
        }

        // QRコードのペアを探す
        foreach (var qr in qrList)
        {
            // ペアが見つかった
            var qrPair = qrList
                .Where(q => q.RoomId == qr.RoomId && q.PositionType != qr.PositionType)
                .Cast<QRData?>()
                .FirstOrDefault();
            if (qrPair != null)
            {
                // ルームIDを返す
                roomId = qr.RoomId;
                // 位置を取得
                Point leftTop, rightBottom;
                if (qr.PositionType == QRPosition.LeftTop)
                {
                    leftTop = qr.Position;
                    rightBottom = qrPair.Value.Position;
                }
                else
                {
                    leftTop = qrPair.Value.Position;
                    rightBottom = qr.Position;
                }
                // 矩形を返す
                double margin = 16;
                rect = new Rect(
                    new WPoint(SystemParameters.VirtualScreenLeft + leftTop.X - margin, SystemParameters.VirtualScreenTop + leftTop.Y - margin),
                    new WPoint(SystemParameters.VirtualScreenLeft + rightBottom.X + margin, SystemParameters.VirtualScreenTop + rightBottom.Y + margin)
                );
                return true;
            }
        }

        // ペアが見つからなかった
        return false;
    }
}
