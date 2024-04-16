namespace ModernWpf.Controls;

public class Image : Control
{
    /// <summary>
    /// 图片路径
    /// </summary>
    public string Source
    {
        get { return (string)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(string), typeof(Image), new PropertyMetadata(default));
}
