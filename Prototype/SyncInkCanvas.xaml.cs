using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace screen_draw_together.Prototype
{
    /// <summary>
    /// SyncInkCanvas.xaml の相互作用ロジック
    /// </summary>
    public partial class SyncInkCanvas : Window
    {
        public SyncInkCanvas()
        {
            InitializeComponent();

            InkCanvas1.syncPlugin.StylusDown += StylusPlugin_StylusDown;
            InkCanvas1.syncPlugin.StylusMove += StylusPlugin_StylusMove;
            InkCanvas1.syncPlugin.StylusUp += StylusPlugin_StylusUp;
        }

        private Stroke? stroke;

        private void StylusPlugin_StylusDown(RawStylusInput e)
        {
            stroke = null;
        }

        private void StylusPlugin_StylusMove(RawStylusInput e)
        {
            StylusPointCollection points = e.GetStylusPoints();

            foreach (var point in points)
            {
                if (stroke == null)
                {
                    stroke = new(new StylusPointCollection(new List<StylusPoint>() { new StylusPoint(point.X, point.Y) }))
                    {
                        DrawingAttributes = InkCanvas2.DefaultDrawingAttributes
                    };
                    
                    InkCanvas2.Strokes.Add(stroke);
                }
                else
                {
                    stroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
                }
            }
        }

        private void StylusPlugin_StylusUp(RawStylusInput e)
        {
            stroke = null;
        }

        public async void Sync()
        {
            await Task.Delay(5000);
        }
    }
}
