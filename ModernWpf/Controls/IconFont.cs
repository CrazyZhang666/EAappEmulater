namespace ModernWpf.Controls;

public class IconFont : Control
{
    /// <summary>
    /// 字体图标
    /// </summary>
    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(string), typeof(IconFont), new PropertyMetadata(default));
}
