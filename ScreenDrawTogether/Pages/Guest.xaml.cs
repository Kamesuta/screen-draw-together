using Firebase.Auth;
using FireSharp.Core.Exceptions;
using ScreenDrawTogether.Common;
using ScreenDrawTogether.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ScreenDrawTogether.Pages;

/// <summary>
/// Guest.xaml の相互作用ロジック
/// </summary>
public partial class Guest : Page
{
    // キャンバス
    private DrawSyncInkCanvas? _syncCanvas;
    // ピア
    DrawNetworkPeer.Guest? _peer;

    public Guest()
    {
        InitializeComponent();

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

        // ゲストピアを作成
        _peer = new DrawNetworkPeer.Guest(routingInfo, auth);

        // 切断時
        _peer.OnHostClosed += (state) =>
        {
            // UIスレッドで実行
            Dispatcher.BeginInvoke(new Action(() =>
            {
                NotifyError("通信が切断されました");
                // TODO: 切断された時メインメニューに戻す
                StopJoin();
            }));
        };
        // 接続時
        _peer.OnConnected += () =>
        {
            // UIスレッドで実行
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GuestButton.Content = "接続完了";
                GuestButton.IsEnabled = false;
            }));
        };

        // ウィンドウを作成
        _syncCanvas = new(_peer);

        // 自動的に招待開始
        StartJoin();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        // ウィンドウを閉じる
        _syncCanvas?.Close();
        _syncCanvas = null;

        // ピアを閉じる
        _peer?.Dispose();
        _peer = null;
    }

    private void GuestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_peer == null)
        {
            return;
        }

        if (_peer.IsSignaling)
        {
            // シグナリング中なら停止
            StopJoin();
        }
        else
        {
            // シグナリング中でなければ開始
            StartJoin();
        }
    }

    /// <summary>
    /// 招待を開始
    /// </summary>
    private async void StartJoin()
    {
        if (_peer == null)
        {
            return;
        }

        // QRコードを認識
        if (!DrawQR.TryReadShareRoomQR(out Rect rect, out string roomId))
        {
            NotifyError("QRコードの認識に失敗しました。");
            return;
        }

        // ルームIDを設定
        _peer.RoomId = roomId;
        // キャンバスの位置を設定
        _syncCanvas?.SetRect(rect);
        _syncCanvas?.Show();

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
        catch (ObjectDisposedException)
        {
            NotifyError("接続に失敗しました。\n接続中にキャンセルされました。");
            return;
        }

        // ボタン名を変更
        GuestButton.Content = "接続中...";
    }

    /// <summary>
    /// 招待を停止
    /// </summary>
    private void StopJoin()
    {
        // シグナリングを停止
        _peer?.StopSignaling();

        // ボタン名を変更
        GuestButton.Content = "参加する";
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
