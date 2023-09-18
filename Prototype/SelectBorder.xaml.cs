using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace screen_draw_together.Prototype
{
    /// <summary>
    /// SelectBorder.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectBorder : Window
    {
        public SelectBorder()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 範囲の位置、大きさを変える。アニメーション付き
        /// </summary>
        /// <param name="rect">新たな範囲</param>
        /// <see cref="https://stackoverflow.com/a/51388226"/>
        public void SetRect(Rect rect)
        {
            var myRectAnimation = new RectAnimation
            {
                Duration = TimeSpan.FromSeconds(0.1),
            };

            SelectRect.BeginAnimation(RectangleGeometry.RectProperty, myRectAnimation);
            SelectRectBorder.BeginAnimation(RectangleGeometry.RectProperty, myRectAnimation);

            SelectRect.Rect = rect;
            SelectRectBorder.Rect = rect;
        }
    }

    /// <summary>
    /// 長方形コンバーター
    /// </summary>
    /// <see cref="https://stackoverflow.com/a/59390743"/>
    public class RectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new Rect(0, 0, (double)values[0], (double)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
