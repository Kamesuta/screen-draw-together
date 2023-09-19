﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenDrawTogether.Prototype
{
    /// <summary>
    /// AlphaOverlay.xaml の相互作用ロジック
    /// </summary>
    public partial class AlphaOverlay : Window
    {
        protected const int GWL_EXSTYLE = (-20);
        protected const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwLong);

        public AlphaOverlay()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            SetClickable(false);
        }

        public void SetClickable(bool clickable)
        {
            // WPF で オーバーレイ表示をする
            // https://qiita.com/SUIMA/items/ea9faeda750248d57306

            //WindowHandle(Win32) を取得
            var handle = new WindowInteropHelper(this).Handle;

            //クリックをスルー
            int extendStyle = GetWindowLong(handle, GWL_EXSTYLE);
            if (clickable)
            {
                extendStyle &= ~WS_EX_TRANSPARENT; //フラグの削除
            }
            else
            {
                extendStyle |= WS_EX_TRANSPARENT; //フラグの追加
            }
            _ = SetWindowLong(handle, GWL_EXSTYLE, extendStyle);
        }
    }
}
