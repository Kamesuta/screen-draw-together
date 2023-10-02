using ScreenDrawTogether.Common;
using System.Windows;
using System.Windows.Controls;
using ScreenDrawTogether.Core;
using System.Windows.Threading;
using System;
using System.Threading.Tasks;
using FireSharp.Core.Exceptions;
using Firebase.Auth;

namespace ScreenDrawTogether.Pages;

/// <summary>
/// Host.xaml の相互作用ロジック
/// </summary>
public partial class Host : Page
{
    // 範囲
    private readonly HWndRect _hWndRect;
    // キャンバス
    private DrawSyncInkCanvas? _syncCanvas;
    // ピア
    DrawNetworkPeer? _peer;
    // タイマー
    private DispatcherTimer? _timer;
    // スタート時間
    private DateTime? _startTime;

    public Host(HWndRect hWndRect)
    {
        InitializeComponent();
        _hWndRect = hWndRect;

        // セットアップ
        Setup();
    }

    private async void Setup()
    {
        // 接続情報
        DrawNetworkRoutingInfo routingInfo = DrawNetworkRoutingInfo.Default;

        DrawNetworkAuth auth;
        try
        {
            // ログイン
            auth = await DrawNetworkAuth.Login(routingInfo);
        }
        catch (FirebaseAuthException)
        {
            NotifyError("シグナリングサービスへのログインに失敗しました。");
            return;
        }

        // ホストピアを作成
        _peer = new DrawNetworkPeer.Host(routingInfo, auth);

        // 接続時
        _peer.OnConnected += () =>
        {
            // UIスレッドで実行
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 連続招待が有効でない場合は停止
                if (ContinueInviteCheckbox.IsChecked == false)
                {
                    StopInvite();
                }
            }));
        };

        // ウィンドウを表示
        _syncCanvas = new(_peer);
        _syncCanvas.ToClickThroughWindow();
        _syncCanvas.SetRect(_hWndRect.Rect);
        _syncCanvas.Show();

        // 自動的に招待開始
        StartInvite();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        StopInvite();

        // ウィンドウを閉じる
        _syncCanvas?.Close();
        _syncCanvas = null;

        // ピアを閉じる
        _peer?.Dispose();
        _peer = null;
    }

    // 招待開始/終了ボタン
    private void HostButton_Click(object sender, RoutedEventArgs e)
    {
        if (_peer == null)
        {
            return;
        }

        if (_peer.IsSignaling)
        {
            // シグナリング中なら停止
            StopInvite();
        }
        else
        {
            // シグナリング中でなければ開始
            StartInvite();
        }
    }

    /// <summary>
    /// 招待を開始
    /// </summary>
    private async void StartInvite()
    {
        if (_peer == null)
        {
            return;
        }

        // シグナリング開始
        try
        {
            await _peer.StartSignaling();
        }
        catch (FirebaseException)
        {
            NotifyError("接続に失敗しました。\nホストの共有が終了している可能性があります。");
            return;
        }

        // QRコードを表示
        _syncCanvas?.SetInviteRoomId(_peer.Auth.ClientId);

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
        // QRコードを非表示
        _syncCanvas?.SetInviteRoomId(null);

        // タイマーを停止
        _timer?.Stop();
        _timer = null;

        // シグナリングを停止
        _peer?.StopSignaling();

        // ボタン名を変更
        HostButton.Content = "友達を招待する";
    }

    /// <summary>
    /// エラーを通知
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private static void NotifyError(string message)
    {
        MessageBox.Show(message, "Screen Draw Together", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
