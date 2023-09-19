using System.Windows;
using ScreenDrawTogether.Common;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// AlphaOverlay.xaml の相互作用ロジック
    /// </summary>
    public partial class AlphaOverlay : Window
    {
        public AlphaOverlay()
        {
            InitializeComponent();
            this.ToClickThroughWindow();
        }
    }
}
