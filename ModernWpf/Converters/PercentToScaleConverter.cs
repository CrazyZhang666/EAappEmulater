using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModernWpf.Converters
{
    public class PercentToScaleConverter : IMultiValueConverter, IValueConverter
    {
        // IValueConverter 实现（原有逻辑，用于ScaleTransform）
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0d;
            if (value is double d)
            {
                if (double.IsNaN(d)) return 0d;
                return System.Math.Max(0, System.Math.Min(1, d / 100d));
            }
            if (double.TryParse(value.ToString(), out var v))
            {
                return System.Math.Max(0, System.Math.Min(1, v / 100d));
            }
            return 0d;
        }

        // IMultiValueConverter 实现（新增，用于Width计算）
        public object Convert(object[] values, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 0d;

            // values[0] = ActualWidth (总宽度)
            // values[1] = Progress (百分比 0-100)

            double totalWidth = 0;
            double progress = 0;

            if (values[0] is double width)
                totalWidth = width;
            else if (values[0] != null && double.TryParse(values[0].ToString(), out var w))
                totalWidth = w;

            if (values[1] is double prog)
                progress = prog;
            else if (values[1] != null && double.TryParse(values[1].ToString(), out var p))
                progress = p;

            // 计算实际宽度
            var percent = System.Math.Max(0, System.Math.Min(100, progress)) / 100d;
            return totalWidth * percent;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
