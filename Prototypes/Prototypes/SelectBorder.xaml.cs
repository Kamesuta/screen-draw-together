using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ScreenDrawTogether.Common;

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
        private readonly DispatcherTimer _timer = new()
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
            _rectList = WindowRectUtility.GetWindowRectList(new WindowInteropHelper(this).Handle);
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
            var rect = WindowRectUtility.GetWindowRectFromListOnCursor(_rectList);
            // 取得できなかったら何もしない、更新されていない場合も何もしない
            if (rect == Rect.Empty || rect == _rect) return;
            _rect = rect;

            // 範囲を更新
            SetRect(rect);
        }
    }
}
