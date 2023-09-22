using System.Windows.Controls;
using System.Windows.Input.StylusPlugIns;

namespace ScreenDrawTogether.Common;

/// <summary>
/// 描いた内容を共有できるInkCanvas
/// </summary>
public class DrawInkCanvas : InkCanvas
{
    /// <summary>
    /// 描いた際のイベント
    /// </summary>
    /// <param name="input">描いた内容</param>
    public delegate void CanvasStylusEventHandler(RawStylusInput input);

    /// <summary>
    /// 描き始めた際に呼ばれるイベント
    /// </summary>
    public event CanvasStylusEventHandler CanvasStylusDown = delegate { };

    /// <summary>
    /// 描いている途中に毎フレーム呼ばれるイベント
    /// </summary>
    public event CanvasStylusEventHandler CanvasStylusMove = delegate { };

    /// <summary>
    /// 描き終わった際に呼ばれるイベント
    /// </summary>
    public event CanvasStylusEventHandler CanvasStylusUp = delegate { };

    /// <summary>
    /// 描いたイベントを取得するプラグイン
    /// </summary>
    private readonly StylusEventPlugin plugin;

    // コンストラクタ
    public DrawInkCanvas()
        : base()
    {
        // プラグインを作成
        plugin = new StylusEventPlugin(this);

        // DynamicRendererの前にプラグインを挿入
        StylusPlugIns.Insert(StylusPlugIns.IndexOf(DynamicRenderer), plugin);
    }

    /// <summary>
    /// 描いたイベントを取得するStylusプラグイン
    /// </summary>
    private class StylusEventPlugin : StylusPlugIn
    {
        private readonly DrawInkCanvas _canvas;

        public StylusEventPlugin(DrawInkCanvas canvas)
        {
            _canvas = canvas;
        }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            // データを編集する前にベースを呼び出す
            base.OnStylusDown(rawStylusInput);

            // イベント発火
            _canvas.CanvasStylusDown(rawStylusInput);
        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            // データを編集する前にベースを呼び出す
            base.OnStylusMove(rawStylusInput);

            // イベント発火
            _canvas.CanvasStylusMove(rawStylusInput);
        }

        protected override void OnStylusUp(RawStylusInput rawStylusInput)
        {
            // データを編集する前にベースを呼び出す
            base.OnStylusUp(rawStylusInput);

            // イベント発火
            _canvas.CanvasStylusUp(rawStylusInput);
        }
    }
}
