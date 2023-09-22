using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Point = System.Drawing.Point;

namespace ScreenDrawTogether.Common;

/// <summary>
/// ウィンドウハンドルとウィンドウの範囲
/// </summary>
public struct HWndRect
{
    /// <summary>
    /// ウィンドウハンドル
    /// </summary>
    public IntPtr HWnd { get; set; }

    /// <summary>
    /// ウィンドウの範囲
    /// </summary>
    public Rect Rect { get; set; }

    /// <summary>
    /// なし
    /// </summary>
    public static HWndRect Empty => new() { HWnd = IntPtr.Zero, Rect = Rect.Empty };
}

/// <summary>
/// ウィンドウの範囲を取得するクラス
/// </summary>
public static partial class WindowRectUtility
{
    /// <summary>
    /// カーソルの位置にあるウィンドウの範囲を取得
    /// </summary>
    /// <param name="rectList">ウィンドウハンドル&範囲のリスト</param>
    /// <returns>ウィンドウハンドル&範囲</returns>
    public static HWndRect GetWindowRectFromListOnCursor(List<HWndRect> rectList)
    {
        // カーソルの位置を取得
        if (!NativeMethods.GetCursorPos(out var p)) return HWndRect.Empty;

        return rectList.FirstOrDefault(
            // カーソルの位置にあるウィンドウを探す
            (hWndRect) => hWndRect.Rect.Left <= p.X && p.X <= hWndRect.Rect.Right && hWndRect.Rect.Top <= p.Y && p.Y <= hWndRect.Rect.Bottom,
            HWndRect.Empty
        );
    }

    /// <summary>
    /// ウィンドウの範囲リストを取得
    /// </summary>
    /// <param name="selfHWnd">自身のウィンドウハンドル</param>
    /// <returns>ウィンドウハンドル&範囲のリスト</returns>
    public static List<HWndRect> GetWindowRectList(IntPtr selfHWnd)
    {
        // デスクトップウィンドウ
        var shellHWnd = NativeMethods.GetShellWindow();
        // タスクトレイウィンドウ
        var trayHWnd = NativeMethods.FindWindowA("Shell_TrayWnd", null);

        // ウィンドウをスキャン
        List<HWndRect> resultRects = new();
        bool ScanWindow(IntPtr hWnd, IntPtr lparam)
        {
            // 見えないウィンドウは無視
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            // 自身は無視
            if (hWnd == selfHWnd) return true;
            // デスクトップは無視
            if (hWnd == shellHWnd) return true;
            // タスクトレイは無視
            if (hWnd == trayHWnd) return true;

            // 隠されたアプリを無視 (UWPアプリ や Microsoft Text Input Application など)
            // 参考: https://www.natsuneko.blog/entry/2018/08/09/enum-windows-exclude-invisible-uwp-app
            if (NativeMethods.DwmGetWindowAttributeBool(hWnd, NativeMethods.DWMWA_CLOAKED, out bool cloaked, Marshal.SizeOf(typeof(bool))) != 0) return true;
            if (cloaked) return true;

            // ウィンドウの範囲を取得
            if (NativeMethods.DwmGetWindowAttributeRect(hWnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out NativeMethods.RECT rect, Marshal.SizeOf(typeof(NativeMethods.RECT))) != 0) return true;
            // 空のウィンドウは無視
            if (rect.Left == rect.Right || rect.Top == rect.Bottom) return true;

            // ウィンドウの範囲記録して終了
            resultRects.Add(new HWndRect() { HWnd = hWnd, Rect = new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top) });
            return true;
        }
        NativeMethods.EnumWindows(ScanWindow, IntPtr.Zero);

        // ウィンドウの範囲をRectに変換して返す
        return resultRects;
    }

    /// <summary>
    /// ディスプレイの範囲リストを取得
    /// </summary>
    /// <returns>ウィンドウハンドル&範囲のリスト</returns>
    public static List<HWndRect> GetMonitorRectList()
    {
        return WpfScreenHelper.Screen.AllScreens.Select((screen) => new HWndRect() { HWnd = IntPtr.Zero, Rect = screen.Bounds }).ToList();
    }

    /// <summary>
    /// ウィンドウのプレビューを取得
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Bitmap PrintWindow(HWndRect hWndRect)
    {
        var captureBmp = new Bitmap((int)hWndRect.Rect.Width, (int)hWndRect.Rect.Height);
        using var captureGraphics = Graphics.FromImage(captureBmp);

        // モニターをキャプチャ
        captureGraphics.CopyFromScreen(new Point((int)hWndRect.Rect.Left, (int)hWndRect.Rect.Top), new Point(0, 0), captureBmp.Size);

        return captureBmp;
    }

    /// <summary>
    /// P/Invokeクラス
    /// </summary>
    private partial class NativeMethods
    {
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        public const int DWMWA_CLOAKED = 14;

        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetCursorPos(out System.Drawing.Point p);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);

        [LibraryImport("user32.dll")]
        public static partial long GetWindowLongA(IntPtr hWnd, int nIndex);

        [LibraryImport("user32.dll")]
        public static partial IntPtr GetShellWindow();

        [LibraryImport("user32.dll")]
        public static partial IntPtr FindWindowA([MarshalAs(UnmanagedType.LPStr)] string? lpClassName, [MarshalAs(UnmanagedType.LPStr)] string? lpWindowName);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindowVisible(IntPtr hWnd);

        //// デバッグ用
        //[LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        //public static partial int GetWindowTextW(IntPtr hWnd, Span<char> lpString, int nMaxCount);

        /// <summary>
        /// ウィンドウの属性を取得
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        /// <param name="dwAttribute">ウィンドウ属性</param>
        /// <param name="rect">範囲の出力</param>
        /// <param name="cbAttribute">RECTのサイズ sizeof(RECT) を指定</param>
        /// <returns>戻り値が0なら成功、0以外ならエラー値</returns>
        /// <see cref="https://gogowaten.hatenablog.com/entry/2020/11/17/004505"/>
        [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        public static partial long DwmGetWindowAttributeRect(IntPtr hWnd, int dwAttribute, out RECT rect, int cbAttribute);

        /// <summary>
        /// ウィンドウの属性を取得
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        /// <param name="dwAttribute">ウィンドウ属性</param>
        /// <param name="rect">範囲の出力</param>
        /// <param name="cbAttribute">boolのサイズ sizeof(bool) を指定</param>
        /// <returns>戻り値が0なら成功、0以外ならエラー値</returns>
        /// <see cref="https://www.natsuneko.blog/entry/2018/08/09/enum-windows-exclude-invisible-uwp-app"/>
        [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        public static partial long DwmGetWindowAttributeBool(IntPtr hWnd, int dwAttribute, [MarshalAs(UnmanagedType.Bool)] out bool boolean, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
    }
}
