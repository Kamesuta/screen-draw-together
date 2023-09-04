using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input;

namespace screen_draw_together.Prototype
{
    public class DrawInkCanvas : InkCanvas
    {
        public readonly SyncPlugin syncPlugin = new();

        public DrawInkCanvas()
            : base()
        {
            // DynamicRendererの前に同期プラグインを挿入
            StylusPlugIns.Insert(StylusPlugIns.IndexOf(DynamicRenderer), syncPlugin);
        }

        // A StylusPlugin that restricts the input area.
        public class SyncPlugin : StylusPlugIn
        {
            public delegate void StylusEventHandler(RawStylusInput e);
            public event StylusEventHandler StylusDown = delegate { };
            public event StylusEventHandler StylusMove = delegate { };
            public event StylusEventHandler StylusUp = delegate { };

            protected override void OnStylusDown(RawStylusInput rawStylusInput)
            {
                // データを編集する前にベースを呼び出す
                base.OnStylusDown(rawStylusInput);

                // イベント発火
                StylusDown(rawStylusInput);
            }

            protected override void OnStylusMove(RawStylusInput rawStylusInput)
            {
                // Call the base class before modifying the data.
                base.OnStylusMove(rawStylusInput);

                // イベント発火
                StylusMove(rawStylusInput);
            }

            protected override void OnStylusUp(RawStylusInput rawStylusInput)
            {
                // Call the base class before modifying the data.
                base.OnStylusUp(rawStylusInput);

                // イベント発火
                StylusUp(rawStylusInput);
            }
        }
    }
}
