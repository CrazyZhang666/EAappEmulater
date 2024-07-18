namespace ModernWpf.Controls;

public class FormLabel : Control
{
    /// <summary>
    /// 标题宽度
    /// </summary>
    public double TitleWidth
    {
        get { return (double)GetValue(TitleWidthProperty); }
        set { SetValue(TitleWidthProperty, value); }
    }
    public static readonly DependencyProperty TitleWidthProperty =
        DependencyProperty.Register("TitleWidth", typeof(double), typeof(FormLabel), new PropertyMetadata(default));

    /// <summary>
    /// 标题文本
    /// </summary>
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(FormLabel), new PropertyMetadata(default));

    /// <summary>
    /// 内容文本
    /// </summary>
    public string Content
    {
        get { return (string)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(string), typeof(FormLabel), new PropertyMetadata(default));
}
