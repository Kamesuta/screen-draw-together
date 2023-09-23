using ScreenDrawTogether.Common;
using System.Windows;
using System.Windows.Controls;
using ScreenDrawTogether.Core;
using System.Windows.Threading;
using System;
using System.Threading.Tasks;

namespace ScreenDrawTogether.Pages
{
    /// <summary>
    /// Host.xaml の相互作用ロジック
    /// </summary>
    public partial class Host : Page
    {
        // 範囲
        private readonly HWndRect _hWndRect;
        // キャンバス
        private WebRTCSyncInkCanvas? _syncCanvas;
        // タイマー
        private DispatcherTimer? _timer;
        // スタート時間
        private DateTime? _startTime;

        public Host(HWndRect hWndRect)
        {
            InitializeComponent();
            _hWndRect = hWndRect;

            // 自動的に招待を開始
            StartInvite();
        }

        private void HostButton_Click(object sender, RoutedEventArgs e)
        {
            if (_syncCanvas != null)
            {
                StopInvite();
            }
            else
            {
                StartInvite();
            }
        }

        /// <summary>
        /// 招待を開始
        /// </summary>
        private async void StartInvite()
        {
            // 接続情報
            DrawNetworkRoutingInfo routingInfo = DrawNetworkRoutingInfo.Default;
            // ログイン
            DrawNetworkAuth auth = await DrawNetworkAuth.Login(routingInfo);

            // ウィンドウを表示
            _syncCanvas = new(routingInfo, auth, null);
            _syncCanvas.ToClickThroughWindow();
            _syncCanvas.SetRect(_hWndRect.Rect);
            _syncCanvas.Show();

            // エラー時
            _syncCanvas.OnError += (error) =>
            {
                MessageBox.Show(error, "Screen Draw Together", MessageBoxButton.OK, MessageBoxImage.Error);
                StopInvite();
            };
            // 接続時
            _syncCanvas.OnConnected += () =>
            {
                // 連続招待が有効でない場合は停止
                if (ContinueInviteCheckbox.IsChecked == false)
                {
                    StopInvite();
                }
            };

            // ボタン名を変更
            HostButton.Content = "招待を停止する";

            // スタート時間を記録
            _startTime = DateTime.Now;
            // タイマーを開始
            _timer?.Stop();
            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            _timer.Tick += (e, args) =>
            {
                // 経過時間を表示
                var timeDiff = TimeSpan.FromMinutes(5) - (DateTime.Now - _startTime);
                if (timeDiff != null)
                {
                    if (timeDiff.Value.TotalSeconds <= 0)
                    {
                        StopInvite();
                    }
                    else
                    {
                        HostButton.Content = $"招待を停止する: {timeDiff.Value.Minutes:00}:{timeDiff.Value.Seconds:00}";
                    }
                }
            };
            _timer.Start();
        }

        /// <summary>
        /// 招待を停止
        /// </summary>
        private void StopInvite()
        {
            // タイマーを停止
            _timer?.Stop();
            _timer = null;
            // ウィンドウを閉じる
            _syncCanvas?.Close();
            _syncCanvas = null;
            // ボタン名を変更
            HostButton.Content = "友達を招待する";
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            StopInvite();
        }
    }
}
