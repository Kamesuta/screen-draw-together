using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenDrawTogether.Core;

/// <summary>
/// ネットワークのアプリケーション向け部分
/// </summary>
public class DrawNetworkClient : IDisposable
{
    /// <summary>
    /// ネットワークピア
    /// </summary>
    public DrawNetworkPeer Peer { get; }

    /// <summary>
    /// ストロークを離した時
    /// </summary>
    public event Action OnStrokeOff = delegate { };
    /// <summary>
    /// ストロークを書き足した時
    /// </summary>
    public event Action<IEnumerable<DrawPoint>> OnStrokeAddPoints = delegate { };

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public DrawNetworkClient(DrawNetworkPeer peer)
    {
        Peer = peer;

        // メッセージ受信時のイベントを登録
        Peer.OnMessage += OnMessage;
    }

    // ファイナライズ
    public void Dispose()
    {
        Peer.OnMessage -= OnMessage;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// パケットを送信する
    /// </summary>
    /// <param name="stream">情報</param>
    private void SendPacket(MemoryStream memoryStream)
    {
        // 送信
        Peer?.DataChannels.ForEach(channel => channel.send(memoryStream.GetBuffer()));
    }

    /// <summary>
    /// ストロークを離したパケットを送信する
    /// </summary>
    public void SendStrokeOff()
    {
        // パケットを作成
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        // パケットタイプ
        writer.Write((byte)StrokePacketType.StrokeOff);
        // 送信
        SendPacket(memoryStream);
    }

    /// <summary>
    /// ストロークを書き足したパケットを送信する
    /// </summary>
    /// <param name="points">点</param>
    public void SendStrokeAddPoints(IEnumerable<DrawPoint> points)
    {
        // パケットを作成
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        // パケットタイプ
        writer.Write((byte)StrokePacketType.StrokeAddPoints);
        // ストロークのポイント数
        writer.Write(points.Count());
        // ストロークのポイント
        foreach (var stylusPoint in points)
        {
            writer.Write(stylusPoint.X);
            writer.Write(stylusPoint.Y);
        }
        // 送信
        SendPacket(memoryStream);
    }

    /// <summary>
    /// 他人の入力: メッセージ受信時
    /// </summary>
    /// <param name="dc">データのチャネル</param>
    /// <param name="protocol">プロトコル</param>
    /// <param name="data">データ</param>
    private void OnMessage(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data)
    {
        // バイナリからストロークパケットを復元
        using var stream = new BinaryReader(new MemoryStream(data));
        // パケットタイプを読み込み
        StrokePacketType packetType = (StrokePacketType)stream.ReadByte();

        // パケットタイプによって処理を分岐
        switch (packetType)
        {
            // ストローク開始/終了時はストロークを切る
            case StrokePacketType.StrokeOff:
                {
                    OnStrokeOff();
                }
                break;

            // ストローク移動時はポイントを追加
            case StrokePacketType.StrokeAddPoints:
                {
                    // ストロークのポイント数を読み込み
                    int count = stream.ReadInt32();
                    // ストロークのポイントを読み込み
                    var points = new List<DrawPoint>();
                    for (int i = 0; i < count; i++)
                    {
                        points.Add(new()
                        {
                            X = stream.ReadDouble(),
                            Y = stream.ReadDouble(),
                        });
                    }
                    // ストロークを追加
                    OnStrokeAddPoints(points);
                }
                break;
        }
    }

    /// <summary>
    /// 2次元座標
    /// </summary>
    public struct DrawPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// ストロークパケットタイプ
    /// </summary>
    public enum StrokePacketType
    {
        /// <summary>
        /// パケットなし
        /// </summary>
        None,
        /// <summary>
        /// 同期情報
        /// </summary>
        SyncInfo,
        /// <summary>
        /// ストローク終了
        /// </summary>
        StrokeOff,
        /// <summary>
        /// ストローク移動
        /// </summary>
        StrokeAddPoints,
    }
}
