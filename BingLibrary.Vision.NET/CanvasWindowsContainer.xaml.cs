using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
using BingLibrary.Vision.Models;

namespace BingLibrary.Vision
{

    /// <summary>
    /// CanvasWindowsContainer.xaml 的交互逻辑
    /// </summary>
    public partial class CanvasWindowsContainer : System.Windows.Controls.UserControl
    {
        private bool _updatingSize = false;

        public CanvasWindowsContainer()
        {
            InitializeComponent();
            this.Loaded += CanvasWindowsContainer_Loaded;
            this.SizeChanged += CanvasWindowsContainer_SizeChanged;
        }

        private void CanvasWindowsContainer_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCanvasSizeFromParent();
        }

        private void CanvasWindowsContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_updatingSize)
            {
                _updatingSize = true;
                try
                {
                    UpdateCanvasSizeFromParent();
                }
                finally
                {
                    _updatingSize = false;
                }
            }
        }

        private void UpdateCanvasSizeFromParent()
        {
            double availableWidth = this.ActualWidth;
            double availableHeight = this.ActualHeight;

            if (availableWidth <= 0 || availableHeight <= 0)
                return;

            availableWidth -= 12; // 调整边框
            availableHeight -= 12;

            if (double.IsNaN(CanvasWidth) || CanvasWidth <= 0 ||
                Math.Abs(CanvasWidth - availableWidth) > 0.5)
            {
                CanvasWidth = availableWidth > 0 ? availableWidth : 1;
            }

            if (double.IsNaN(CanvasHeight) || CanvasHeight <= 0 ||
                Math.Abs(CanvasHeight - availableHeight) > 0.5)
            {
                CanvasHeight = availableHeight > 0 ? availableHeight : 1;
            }
        }

        // 画布宽度依赖属性
        public static readonly DependencyProperty CanvasWidthProperty =
            DependencyProperty.Register("CanvasWidth", typeof(double), typeof(CanvasWindowsContainer),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public double CanvasWidth
        {
            get { return (double)GetValue(CanvasWidthProperty); }
            set { SetValue(CanvasWidthProperty, value); }
        }

        // 画布高度依赖属性
        public static readonly DependencyProperty CanvasHeightProperty =
            DependencyProperty.Register("CanvasHeight", typeof(double), typeof(CanvasWindowsContainer),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public double CanvasHeight
        {
            get { return (double)GetValue(CanvasHeightProperty); }
            set { SetValue(CanvasHeightProperty, value); }
        }

        // 窗口项依赖属性
        public static readonly DependencyProperty WindowItemsProperty =
            DependencyProperty.Register("WindowItems", typeof(IEnumerable), typeof(CanvasWindowsContainer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public IEnumerable WindowItems
        {
            get { return (IEnumerable)GetValue(WindowItemsProperty); }
            set { SetValue(WindowItemsProperty, value); }
        }

        // 支持自动调整大小
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            if ((double.IsNaN(CanvasWidth) || CanvasWidth <= 0) && !double.IsInfinity(constraint.Width))
                CanvasWidth = constraint.Width;

            if ((double.IsNaN(CanvasHeight) || CanvasHeight <= 0) && !double.IsInfinity(constraint.Height))
                CanvasHeight = constraint.Height;

            return base.MeasureOverride(constraint);
        }
    }


    // 相对X坐标转换为绝对X坐标
    public class RelativeToAbsoluteXConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double relativeX && values[1] is double canvasWidth)
            {
                return relativeX * canvasWidth;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 相对Y坐标转换为绝对Y坐标
    public class RelativeToAbsoluteYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double relativeY && values[1] is double canvasHeight)
            {
                return relativeY * canvasHeight;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 相对宽度转换为绝对宽度
    public class RelativeToAbsoluteWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double relativeWidth && values[1] is double canvasWidth)
            {
                return relativeWidth * canvasWidth;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 相对高度转换为绝对高度
    public class RelativeToAbsoluteHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double relativeHeight && values[1] is double canvasHeight)
            {
                return relativeHeight * canvasHeight;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
