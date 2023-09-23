using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScreenDrawTogether.Core;

/// <summary>
/// ネットワークルーティング情報
/// </summary>
public class DrawNetworkRoutingInfo
{
    /// <summary>
    /// Firebase Authentication の API Key
    /// </summary>
    [JsonPropertyName("authApiKey")]
    public string AuthApiKey { get; }
    /// <summary>
    /// Firebase Authentication の Auth Domain
    /// </summary>
    [JsonPropertyName("authDomain")]
    public string AuthDomain { get; }

    /// <summary>
    /// Firebase Realtime Database の URL
    /// </summary>
    [JsonPropertyName("databasePath")]
    public string DatabasePath { get; }

    /// <summary>
    /// TURN サーバーの URL
    /// 「stun:」や「turn:」から始まる、カンマで複数指定可
    /// </summary>
    [JsonPropertyName("relayUrls")]
    public string RelayUrls { get; }
    /// <summary>
    /// TURN サーバーのユーザー名
    /// </summary>
    [JsonPropertyName("relayUsername")]
    public string RelayUsername { get; }
    /// <summary>
    /// TURN サーバーのシークレット
    /// </summary>
    [JsonPropertyName("relaySecret")]
    public string RelaySecret { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="authApiKey">Firebase Authentication の API Key</param>
    /// <param name="authDomain">Firebase Authentication の Auth Domain</param>
    /// <param name="databasePath">Firebase Realtime Database の URL</param>
    /// <param name="relayUrls">TURN サーバーの URL</param>
    /// <param name="relayUsername">TURN サーバーのユーザー名</param>
    /// <param name="relaySecret">TURN サーバーのシークレット</param>
    public DrawNetworkRoutingInfo(string authApiKey, string authDomain, string databasePath, string relayUrls, string relayUsername, string relaySecret)
    {
        AuthApiKey = authApiKey;
        AuthDomain = authDomain;
        DatabasePath = databasePath;
        RelayUrls = relayUrls;
        RelayUsername = relayUsername;
        RelaySecret = relaySecret;
    }

    /// <summary>
    /// デフォルトのネットワークルーティング情報
    /// </summary>
    public static DrawNetworkRoutingInfo Default { get; set; } = new(
        // Firebase Authentication(無料)を使用
        authApiKey: "AIzaSyAOpybQOeDJt17_yVYSaTzx-ZH_h9y5zL8",
        authDomain: "screen-draw-together.firebaseapp.com",
        // Firebase Realtime Database(無料)を使用
        databasePath: "https://screen-draw-together-default-rtdb.asia-southeast1.firebasedatabase.app",
        // Metred Open Relay STUN/TURN サーバー(無料)を使用
        relayUrls: "stun:stun.relay.metered.ca:80," +
                        "turn:a.relay.metered.ca:80," +
                        "turn:a.relay.metered.ca:80?transport=tcp," +
                        "turn:a.relay.metered.ca:443," +
                        "turn:a.relay.metered.ca:443?transport=tcp",
        relayUsername: "dbdc990c4e887b45b0b264d8",
        relaySecret: "2U/sMrrs+RSxLYT8"
    );
}
