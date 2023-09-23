using Firebase.Auth.Repository;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenDrawTogether.Core;

/// <summary>
/// 認証情報
/// </summary>
public class DrawNetworkAuth
{
    /// <summary>
    /// 認証用ロガー
    /// </summary>
    private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Authentication");

    /// <summary>
    /// 永続ログインデータのパス
    /// %AppData%\screen-draw-together\{presetId}
    /// </summary>
    private static readonly string AuthenticationDataPath = "screen-draw-together";

    /// <summary>
    /// クライアントID
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// トークン
    /// </summary>
    public string ClientIdToken { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="clientId">クライアントID</param>
    /// <param name="clientIdToken">トークン</param>
    public DrawNetworkAuth(string clientId, string clientIdToken)
    {
        ClientId = clientId;
        ClientIdToken = clientIdToken;
    }

    /// <summary>
    /// ログインする
    /// </summary>
    /// <param name="routingInfo">接続情報</param>
    /// <param name="presetId">デバッグ用プリセットID</param>
    /// <returns>認証情報</returns>
    public static async Task<DrawNetworkAuth> Login(DrawNetworkRoutingInfo routingInfo, string? presetId = null)
    {
        // FirebaseプロジェクトのAPIキーを使用してFirebase認証コンフィグを作成
        Logger.Info("Start authention with Firebase.");
        var authConfig = new FirebaseAuthConfig
        {
            ApiKey = routingInfo.AuthApiKey,
            AuthDomain = routingInfo.AuthDomain,
            UserRepository = new FileUserRepository($"{AuthenticationDataPath}{(presetId == null ? "" : $"/{presetId}")}"),
        };
        // FirebaseAuthClientを作成
        var authClient = new FirebaseAuthClient(authConfig);

        // ログインを行う
        User user;
        if (authClient.User != null)
        {
            // 既にログイン済みの場合はローカルに保存されたログイン情報を使用
            Logger.Info("Use existing login.");
            user = authClient.User;
        }
        else
        {
            // ログインしていない場合は新規匿名ログイン
            Logger.Info("Signing in anonymously.");
            var cred = await authClient.SignInAnonymouslyAsync();
            user = cred.User;
        }
        // ログインしたユーザーのUIDを取得
        var clientId = user.Uid;
        // トークンを更新して取得
        var clientIdToken = await user.GetIdTokenAsync();
        Logger.Info($"Client ID is {clientId}");

        // 認証情報を返す
        return new DrawNetworkAuth(clientId, clientIdToken);
    }
}
