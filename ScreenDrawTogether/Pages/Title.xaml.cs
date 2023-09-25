using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ScreenDrawTogether.Pages;

/// <summary>
/// Title.xaml の相互作用ロジック
/// </summary>
public partial class Title : Page
{
    public Title()
    {
        InitializeComponent();
    }

    private void HostButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new SelectScreen());
    }

    private void GuestButton_Click(object sender, RoutedEventArgs e)
    {

    }
}
