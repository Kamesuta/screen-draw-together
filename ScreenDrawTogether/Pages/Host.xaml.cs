using ScreenDrawTogether.Common;
using System.Windows;
using System.Windows.Controls;

namespace ScreenDrawTogether.Pages
{
    /// <summary>
    /// Host.xaml の相互作用ロジック
    /// </summary>
    public partial class Host : Page
    {
        private readonly HWndRect _hWndRect;

        public Host(HWndRect hWndRect)
        {
            InitializeComponent();
            _hWndRect = hWndRect;
        }

        private void HostButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
