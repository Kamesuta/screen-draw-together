using Firebase.Auth;
using Firebase.Auth.Repository;
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
    /// Firebase資格情報ユーザー
    /// </summary>
    public User ClientUser { get; }

    /// <summary>
    /// クライアントID
    /// </summary>
    public string ClientId => ClientUser.Uid;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="clientUser">Firebase資格情報ユーザー</param>
    private DrawNetworkAuth(User clientUser)
    {
        ClientUser = clientUser;
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
        User clientUser;
        if (authClient.User != null)
        {
            // 既にログイン済みの場合はローカルに保存されたログイン情報を使用
            Logger.Info("Use existing login.");
            clientUser = authClient.User;
        }
        else
        {
            // ログインしていない場合は新規匿名ログイン
            Logger.Info("Signing in anonymously.");
            var cred = await authClient.SignInAnonymouslyAsync();
            clientUser = cred.User;
        }

        Logger.Info($"Client ID is {clientUser.Uid}");

        // 認証情報を返す
        return new DrawNetworkAuth(clientUser);
    }
}
