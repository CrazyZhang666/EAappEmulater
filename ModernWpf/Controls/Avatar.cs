namespace ModernWpf.Controls;

public class Avatar : Control
{
    /// <summary>
    /// 头像路径
    /// </summary>
    public string Source
    {
        get { return (string)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(string), typeof(Avatar), new PropertyMetadata(default));
}
