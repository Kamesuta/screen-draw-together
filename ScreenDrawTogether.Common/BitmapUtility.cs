using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace ScreenDrawTogether.Common;

/// <summary>
/// ビットマップユーティリティ
/// </summary>
public static class BitmapUtility
{
    /// <summary>
    /// ビットマップをImageSourceに変換する
    /// </summary>
    /// <param name="bitmap">ビットマップ</param>
    /// <returns>ImageSource</returns>
    public static BitmapImage ToImageSource(this Bitmap bitmap)
    {
        using MemoryStream memory = new();

        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
        memory.Position = 0;
        BitmapImage bitmapimage = new();
        bitmapimage.BeginInit();
        bitmapimage.StreamSource = memory;
        bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapimage.EndInit();

        return bitmapimage;
    }
}
