namespace ModernWpf.Controls;

public class WinButton : Button
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
        DependencyProperty.Register("Icon", typeof(string), typeof(WinButton), new PropertyMetadata(default));

    /// <summary>
    /// 鼠标悬浮前景色
    /// </summary>
    public string OverForeground
    {
        get { return (string)GetValue(OverForegroundProperty); }
        set { SetValue(OverForegroundProperty, value); }
    }
    public static readonly DependencyProperty OverForegroundProperty =
        DependencyProperty.Register("OverForeground", typeof(string), typeof(WinButton), new PropertyMetadata(default));

    /// <summary>
    /// 鼠标悬浮背景色
    /// </summary>
    public string OverBackground
    {
        get { return (string)GetValue(OverBackgroundProperty); }
        set { SetValue(OverBackgroundProperty, value); }
    }
    public static readonly DependencyProperty OverBackgroundProperty =
        DependencyProperty.Register("OverBackground", typeof(string), typeof(WinButton), new PropertyMetadata(default));

    /// <summary>
    /// 鼠标按下前景色
    /// </summary>
    public string PressedForeground
    {
        get { return (string)GetValue(PressedForegroundProperty); }
        set { SetValue(PressedForegroundProperty, value); }
    }
    public static readonly DependencyProperty PressedForegroundProperty =
        DependencyProperty.Register("PressedForeground", typeof(string), typeof(WinButton), new PropertyMetadata(default));

    /// <summary>
    /// 鼠标按下背景色
    /// </summary>
    public string PressedBackground
    {
        get { return (string)GetValue(PressedBackgroundProperty); }
        set { SetValue(PressedBackgroundProperty, value); }
    }
    public static readonly DependencyProperty PressedBackgroundProperty =
        DependencyProperty.Register("PressedBackground", typeof(string), typeof(WinButton), new PropertyMetadata(default));
}
