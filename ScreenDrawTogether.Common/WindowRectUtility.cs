using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenDrawTogether.Common;

/// <summary>
/// ウィンドウの範囲を取得するクラス
/// </summary>
public static partial class WindowRectUtility
{
    /// <summary>
    /// カーソルの位置にあるウィンドウの範囲を取得
    /// </summary>
    /// <param name="rectList">ウィンドウの範囲リスト</param>
    /// <returns>ウィンドウの範囲 取得できない場合はnull</returns>
    public static Rect GetWindowRectFromListOnCursor(List<Rect> rectList)
    {
        // カーソルの位置を取得
        if (!NativeMethods.GetCursorPos(out var p)) return Rect.Empty;

        return rectList.FirstOrDefault(
            // カーソルの位置にあるウィンドウを探す
            (rect) => rect.Left <= p.X && p.X <= rect.Right && rect.Top <= p.Y && p.Y <= rect.Bottom,
            Rect.Empty
        );
    }

    /// <summary>
    /// ウィンドウの範囲リストを取得
    /// </summary>
    /// <param name="selfHWnd">自身のウィンドウハンドル</param>
    /// <returns>ウィンドウの範囲リスト</returns>
    public static List<Rect> GetWindowRectList(IntPtr selfHWnd)
    {
        // デスクトップウィンドウ
        var shellHWnd = NativeMethods.GetShellWindow();
        // タスクトレイウィンドウ
        var trayHWnd = NativeMethods.FindWindowA("Shell_TrayWnd", null);

        // ウィンドウをスキャン
        List<Rect> resultRects = new();
        unsafe bool ScanWindow(IntPtr hWnd, IntPtr lparam)
        {
            // 見えないウィンドウは無視
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;
            // 自身は無視
            if (hWnd == selfHWnd) return true;
            // デスクトップは無視
            if (hWnd == shellHWnd) return true;
            // タスクトレイは無視
            if (hWnd == trayHWnd) return true;

            // ウィンドウの範囲を取得
            if (NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out NativeMethods.RECT rect, sizeof(NativeMethods.RECT)) != 0) return true;
            // 空のウィンドウは無視
            if (rect.Left == rect.Right || rect.Top == rect.Bottom) return true;

            // ウィンドウの範囲記録して終了
            resultRects.Add(new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
            return true;
        }
        NativeMethods.EnumWindows(ScanWindow, IntPtr.Zero);

        // ウィンドウの範囲をRectに変換して返す
        return resultRects;
    }

    /// <summary>
    /// ディスプレイの範囲リストを取得
    /// </summary>
    /// <returns>ディスプレイの範囲リスト</returns>
    public static List<Rect> GetMonitorRectList()
    {
        return WpfScreenHelper.Screen.AllScreens.Select((screen) => screen.Bounds).ToList();
    }

    /// <summary>
    /// P/Invokeクラス
    /// </summary>
    private partial class NativeMethods
    {
        public const int WS_CHILD = 0x40000000;
        public const int GWL_STYLE = -16;
        public const int GA_ROOT = 2;
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

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

        /// <summary>
        /// 引数のcbAttributeにこれを渡す
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        /// <param name="dwAttribute">ウィンドウ属性</param>
        /// <param name="rect">範囲の出力</param>
        /// <param name="cbAttribute">はRECTのサイズ sizeof(RECT) を指定</param>
        /// <returns>戻り値が0なら成功、0以外ならエラー値</returns>
        /// <see cref="https://gogowaten.hatenablog.com/entry/2020/11/17/004505"/>
        [LibraryImport("dwmapi.dll")]
        public static partial long DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out RECT rect, int cbAttribute);

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
