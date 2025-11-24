using System.Globalization;
using System.Windows.Data;

namespace ModernWpf.Converters;

public class CookieMaskMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return string.Empty;
        var raw = values[0] as string ?? string.Empty;
        var show = values[1] is bool b && b;
        if (show) return raw;
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        return new string('*', Math.Min(raw.Length, 32));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { Binding.DoNothing, Binding.DoNothing };
    }
}
