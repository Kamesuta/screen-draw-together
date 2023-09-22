﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScreenDrawTogether.Pages
{
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
}
