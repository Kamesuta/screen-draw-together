using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenDrawTogether.Common;

/// <summary>
/// クリック透過なウィンドウにするクラス
/// </summary>
/// <see cref="https://stackoverflow.com/a/2410602"/>
public static partial class ClickThroughWindowUtility
{
    /// <summary>
    /// クリック透過なウィンドウにする
    /// Windowのコンストラクタで呼び出す
    /// </summary>
    /// <param name="window">ウィンドウ</param>
    public static void ToClickThroughWindow(this Window window)
    {
        window.SourceInitialized += (sender, e) =>
        {
            SetClickable(window, false);
        };
    }

    /// <summary>
    /// クリック透過/不透過を切り替える
    /// </summary>
    /// <param name="window">ウィンドウ</param>
    /// <param name="clickable">クリック可能か</param>
    public static void SetClickable(this Window window, bool clickable)
    {
        // WPF で オーバーレイ表示をする
        // https://qiita.com/SUIMA/items/ea9faeda750248d57306

        //WindowHandle(Win32) を取得
        var handle = new WindowInteropHelper(window).Handle;

        //クリックをスルー
        int extendStyle = NativeMethods.GetWindowLongA(handle, NativeMethods.GWL_EXSTYLE);
        if (clickable)
        {
            // フラグの削除 → 透過しない
            extendStyle &= ~NativeMethods.WS_EX_TRANSPARENT; 
        }
        else
        {
            // フラグの追加 → 透過する
            extendStyle |= NativeMethods.WS_EX_TRANSPARENT;
        }
        _ = NativeMethods.SetWindowLongA(handle, NativeMethods.GWL_EXSTYLE, extendStyle);
    }

    /// <summary>
    /// P/Invokeクラス
    /// </summary>
    private partial class NativeMethods
    {
        public const int GWL_EXSTYLE = (-20);
        public const int WS_EX_TRANSPARENT = 0x00000020;

        [LibraryImport("user32")]
        public static partial int GetWindowLongA(IntPtr hWnd, int nIndex);

        [LibraryImport("user32")]
        public static partial int SetWindowLongA(IntPtr hWnd, int nIndex, int dwLong);
    }
}
