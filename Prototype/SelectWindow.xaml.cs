using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace screen_draw_together.Prototype
{
    /// <summary>
    /// SelectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectWindow : Window
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out System.Drawing.Point p);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder title, int size);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT rectangle);

        [DllImport("user32.dll")]
        static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        const int WS_CHILD = 0x40000000;
        const int GWL_STYLE = -16;
        const int GA_ROOT = 2;
        const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        /// <summary>
        /// 引数のcbAttributeにこれを渡す
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        /// <param name="dwAttribute">ウィンドウ属性</param>
        /// <param name="rect">範囲の出力</param>
        /// <param name="cbAttribute">はRECTのサイズ Marshal.SizeOf(typeof(RECT)) を指定</param>
        /// <returns>戻り値が0なら成功、0以外ならエラー値</returns>
        /// <see cref="https://gogowaten.hatenablog.com/entry/2020/11/17/004505"/>
        [DllImport("dwmapi.dll")]
        static extern long DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out RECT rect, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        // キャプチャー有効か
        private bool _isCaptureEnable = false;
        // タイマーのインスタンス
        private DispatcherTimer _timer;


        // オーバーレイ
        private AlphaOverlay _overlayWindow;
        private RECT _rect;


        public SelectWindow()
        {
            InitializeComponent();
            SetupTimer();

            _overlayWindow = new AlphaOverlay();
            _overlayWindow.Show();
        }

        // タイマーを設定する
        private void SetupTimer()
        {
            // タイマーのインスタンスを生成
            _timer = new DispatcherTimer();
            // インターバルを設定
            _timer.Interval = TimeSpan.FromMilliseconds(10);
            // タイマーメソッドを設定
            _timer.Tick += timer_Tick;

            // 画面が閉じられるときに、タイマーを停止
            this.Closing += (sender, e) => _timer.Stop();
        }

        /// <summary>
        /// コントロールからウィンドウのハンドルを取得
        /// </summary>
        /// <param name="hWnd">コントロール or ウィンドウのハンドル</param>
        /// <returns>ウィンドウのハンドル</returns>
        private IntPtr GetWindowFromControl(IntPtr hWnd)
        {
            if ((GetWindowLong(hWnd, GWL_STYLE) & WS_CHILD) == 0)
            {
                return hWnd;
            }
            else
            {
                return GetAncestor(hWnd, GA_ROOT);
            }
        }

        private void timer_Tick(object? sender, EventArgs e)
        {
            if (GetCursorPos(out var p))
            {
                IntPtr hWnd = WindowFromPoint(p);
                if (hWnd != IntPtr.Zero)
                {
                    IntPtr hWindowWnd = GetWindowFromControl(hWnd);

                    // デスクトップは無視
                    if (hWindowWnd == GetShellWindow()) return;
                    // タスクトレイは無視
                    if (hWindowWnd == FindWindow("Shell_TrayWnd", null)) return;

                    //if (GetWindowRect(hWnd, out var rect))
                    if (DwmGetWindowAttribute(hWindowWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, Marshal.SizeOf(typeof(RECT))) == 0)
                    {
                        // 同じの場合更新しない
                        if (rect.Left == _rect.Left && rect.Top == _rect.Top && rect.Right == _rect.Right && rect.Bottom == _rect.Bottom)
                        {
                            return;
                        }
                        _rect = rect;

                        captureWindowText.Content = $"RECT: Left:{rect.Left}, Top:{rect.Top}, Right:{rect.Right}, Bottom:{rect.Bottom}";
                        _overlayWindow.Left = rect.Left;
                        _overlayWindow.Top = rect.Top;
                        _overlayWindow.Width = rect.Right - rect.Left;
                        _overlayWindow.Height = rect.Bottom - rect.Top;
                        return;
                    }
                }
                captureWindowText.Content = "Not found";
            }
        }

        private void captureWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isCaptureEnable)
            {
                // タイマー停止
                _timer.Stop();
                // ステート設定
                Resources["captureWindowState"] = "OFF";
                _isCaptureEnable = false;
            }
            else
            {
                // タイマーを開始
                _timer.Start();
                // ステート設定
                Resources["captureWindowState"] = "ON";
                _isCaptureEnable = true;
            }
        }
    }
}