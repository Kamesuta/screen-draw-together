using System.Windows;
using System.Windows.Controls;

namespace ScreenDrawTogether
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.NavigationService.GoBack();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // TODO: ちゃんとした終了処理。「終了しています...」みたいなのを表示する
            //Application.Current.Shutdown();
        }
    }
}
