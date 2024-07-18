namespace ModernWpf.Converters;

public class StringToImageSource2Converter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var imgPath = (string)value;

        // 当图片路径为空时
        if (string.IsNullOrWhiteSpace(imgPath))
            return null;

        // 需要考虑三种情况
        // 1. 资源图片  pack://
        // 2. 网络图片  https://
        // 3. 本地图片  C:\

        // 资源图片
        if (imgPath.StartsWith("pack://application"))
            goto IMAGE;

        /// 网络图片
        if (imgPath.StartsWith("http://") || imgPath.StartsWith("https://"))
            goto IMAGE;

        // 本地图片
        if (File.Exists(imgPath))
            goto IMAGE;

        // 默认图片路径
        imgPath = "pack://application:,,,/ModernWpf;component/Assets/Images/Avatar.png";

    IMAGE:
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        //bitmapImage.DecodePixelWidth = 100;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //bitmapImage.CreateOptions = BitmapCreateOptions.DelayCreation;
        bitmapImage.UriSource = new Uri(imgPath, UriKind.RelativeOrAbsolute);
        bitmapImage.EndInit();

        return bitmapImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}