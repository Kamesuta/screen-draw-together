using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// SelectBorder.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectBorder : Window
    {
        /// <summary>
        /// 範囲が選択された時
        /// 右クリックでキャンセルされた場合はnullが渡される
        /// </summary>
        public event Action<Rect> OnRectConfirmed = delegate { };

        // タイマーのインスタンス
        private DispatcherTimer _timer = new()
        {
            // インターバルを設定
            Interval = TimeSpan.FromMilliseconds(10)
        };
        // ウィンドウ範囲リスト
        private List<Rect> _rectList = new();
        // 現在の範囲
        private Rect _rect;

        public SelectBorder()
        {
            InitializeComponent();
            // タイマーメソッドを設定
            _timer.Tick += Timer_Tick;
            // タイマーを開始
            _timer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // ウィンドウ範囲リストを取得
            _rectList = WindowNativeMethods.GetWindowRectList(new WindowInteropHelper(this).Handle);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ウィンドウが閉じられるときに、タイマーを停止
            _timer.Stop();
        }

        /// <summary>
        /// 範囲の位置、大きさを変える。アニメーション付き
        /// </summary>
        /// <param name="rect">新たな範囲</param>
        public void SetRect(Rect rect)
        {
            Left = rect.Left;
            Top = rect.Top;
            Width = rect.Width;
            Height = rect.Height;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 左クリックで範囲を確定
                OnRectConfirmed(_rect);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                // 右クリックでキャンセル
                OnRectConfirmed(Rect.Empty);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // カーソルの位置にあるウィンドウの範囲を取得
            var rect = WindowNativeMethods.GetWindowRectFromListOnCursor(_rectList);
            // 取得できなかったら何もしない、更新されていない場合も何もしない
            if (rect == Rect.Empty || rect == _rect) return;
            _rect = rect;

            // 範囲を更新
            SetRect(rect);
        }

        /// <summary>
        /// P/Invokeクラス
        /// </summary>
        internal partial class WindowNativeMethods
        {
            private const int WS_CHILD = 0x40000000;
            private const int GWL_STYLE = -16;
            private const int GA_ROOT = 2;
            private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

            private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static partial bool GetCursorPos(out System.Drawing.Point p);

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static partial bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);

            [LibraryImport("user32.dll")]
            private static partial long GetWindowLongA(IntPtr hWnd, int nIndex);

            [LibraryImport("user32.dll")]
            private static partial IntPtr GetShellWindow();

            [LibraryImport("user32.dll")]
            private static partial IntPtr FindWindowA([MarshalAs(UnmanagedType.LPStr)] string? lpClassName, [MarshalAs(UnmanagedType.LPStr)] string? lpWindowName);

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static partial bool IsWindowVisible(IntPtr hWnd);

            /// <summary>
            /// 引数のcbAttributeにこれを渡す
            /// </summary>
            /// <param name="hWnd">ウィンドウハンドル</param>
            /// <param name="dwAttribute">ウィンドウ属性</param>
            /// <param name="rect">範囲の出力</param>
            /// <param name="cbAttribute">はRECTのサイズ Marshal.SizeOf(typeof(RECT)) を指定</param>
            /// <returns>戻り値が0なら成功、0以外ならエラー値</returns>
            /// <see cref="https://gogowaten.hatenablog.com/entry/2020/11/17/004505"/>
            [LibraryImport("dwmapi.dll")]
            private static partial long DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out RECT rect, int cbAttribute);

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT
            {
                public int Left { get; set; }
                public int Top { get; set; }
                public int Right { get; set; }
                public int Bottom { get; set; }
            }

            /// <summary>
            /// カーソルの位置にあるウィンドウの範囲を取得
            /// </summary>
            /// <param name="rectList">ウィンドウの範囲リスト</param>
            /// <returns>ウィンドウの範囲 取得できない場合はnull</returns>
            public static Rect GetWindowRectFromListOnCursor(List<Rect> rectList)
            {
                // カーソルの位置を取得
                if (!GetCursorPos(out var p)) return Rect.Empty;

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
                var shellHWnd = GetShellWindow();
                // タスクトレイウィンドウ
                var trayHWnd = FindWindowA("Shell_TrayWnd", null);

                // ウィンドウをスキャン
                List<Rect> resultRects = new();
                EnumWindows((hWnd, lparam) =>
                {
                    // 見えないウィンドウは無視
                    if (!IsWindowVisible(hWnd)) return true;
                    // 自身は無視
                    if (hWnd == selfHWnd) return true;
                    // デスクトップは無視
                    if (hWnd == shellHWnd) return true;
                    // タスクトレイは無視
                    if (hWnd == trayHWnd) return true;

                    // ウィンドウの範囲を取得
                    if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, Marshal.SizeOf(typeof(RECT))) != 0) return true;
                    // 空のウィンドウは無視
                    if (rect.Left == rect.Right || rect.Top == rect.Bottom) return true;

                    // ウィンドウの範囲記録して終了
                    resultRects.Add(new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
                    return true;
                }, IntPtr.Zero);

                // ウィンドウの範囲をRectに変換して返す
                return resultRects;
            }
        }
    }
}
