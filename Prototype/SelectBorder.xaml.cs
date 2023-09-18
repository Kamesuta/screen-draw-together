using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace screen_draw_together.Prototype
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
        public event Action<Rect?> OnRectConfirmed = delegate { };

        // タイマーのインスタンス
        private DispatcherTimer _timer = new()
        {
            // インターバルを設定
            Interval = TimeSpan.FromMilliseconds(10)
        };
        // 現在の範囲
        private Rect? _rect;

        public SelectBorder()
        {
            InitializeComponent();
            // タイマーメソッドを設定
            _timer.Tick += Timer_Tick;
            // タイマーを開始
            _timer.Start();
            // ウィンドウの大きさを全画面を覆う範囲にする
            SetWindowSizeToOverlay();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ウィンドウが閉じられるときに、タイマーを停止
            _timer.Stop();
        }

        /// <summary>
        /// オーバーレイ用にウィンドウの大きさを全画面を覆う範囲にする
        /// </summary>
        public void SetWindowSizeToOverlay()
        {
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        /// <summary>
        /// 範囲の位置、大きさを変える。アニメーション付き
        /// </summary>
        /// <param name="rect">新たな範囲</param>
        /// <see cref="https://stackoverflow.com/a/51388226"/>
        public void SetRect(Rect rect)
        {
            // アニメーションを設定
            var myRectAnimation = new RectAnimation
            {
                Duration = TimeSpan.FromSeconds(0.1),
            };

            // アニメーションを開始
            SelectRect.BeginAnimation(RectangleGeometry.RectProperty, myRectAnimation);
            SelectRectBorder.BeginAnimation(RectangleGeometry.RectProperty, myRectAnimation);

            // 範囲を更新
            SelectRect.Rect = rect;
            SelectRectBorder.Rect = rect;
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
                OnRectConfirmed(null);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // カーソルの位置にあるウィンドウの範囲を取得
            var rect = WindowNativeMethods.GetWindowRectOnCursor();
            // 取得できなかったら何もしない、更新されていない場合も何もしない
            if (rect is null || rect == _rect) return;
            _rect = rect;

            // 範囲を更新
            SetRect(rect.Value);
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

            [LibraryImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static partial bool GetCursorPos(out System.Drawing.Point p);

            [LibraryImport("user32.dll")]
            private static partial IntPtr WindowFromPoint(System.Drawing.Point p);

            [LibraryImport("user32.dll")]
            private static partial long GetWindowLongA(IntPtr hWnd, int nIndex);

            [LibraryImport("user32.dll")]
            private static partial IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

            [LibraryImport("user32.dll")]
            private static partial IntPtr GetShellWindow();

            [LibraryImport("user32.dll")]
            private static partial IntPtr FindWindowA([MarshalAs(UnmanagedType.LPStr)] string? lpClassName, [MarshalAs(UnmanagedType.LPStr)] string? lpWindowName);

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
            /// コントロールからウィンドウのハンドルを取得
            /// </summary>
            /// <param name="hWnd">コントロール or ウィンドウのハンドル</param>
            /// <returns>ウィンドウのハンドル</returns>
            private static IntPtr GetWindowFromControl(IntPtr hWnd)
            {
                if ((GetWindowLongA(hWnd, GWL_STYLE) & WS_CHILD) == 0)
                {
                    return hWnd;
                }
                else
                {
                    return GetAncestor(hWnd, GA_ROOT);
                }
            }

            /// <summary>
            /// カーソルの位置にあるウィンドウの範囲を取得
            /// </summary>
            /// <returns>ウィンドウの範囲 取得できない場合はnull</returns>
            public static Rect? GetWindowRectOnCursor()
            {
                // カーソルの位置を取得
                if (!GetCursorPos(out var p)) return null;

                // カーソルの位置にあるコントロールのハンドルを取得
                IntPtr hWnd = WindowFromPoint(p);
                if (hWnd == IntPtr.Zero) return null;

                // コントロールからウィンドウのハンドルを取得
                IntPtr hWindowWnd = GetWindowFromControl(hWnd);

                // デスクトップは無視
                if (hWindowWnd == GetShellWindow()) return null;
                // タスクトレイは無視
                if (hWindowWnd == FindWindowA("Shell_TrayWnd", null)) return null;

                // ウィンドウの範囲を取得
                if (DwmGetWindowAttribute(hWindowWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, Marshal.SizeOf(typeof(RECT))) != 0) return null;

                // ウィンドウの範囲をRectに変換して返す
                return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
        }
    }

    /// <summary>
    /// 長方形コンバーター
    /// </summary>
    /// <see cref="https://stackoverflow.com/a/59390743"/>
    public class RectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new Rect(0, 0, (double)values[0], (double)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
