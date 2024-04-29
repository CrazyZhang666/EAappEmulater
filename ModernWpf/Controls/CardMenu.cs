namespace ModernWpf.Controls;

public class CardMenu : Control
{
    /// <summary>
    /// 卡片图片
    /// </summary>
    public ImageSource Source
    {
        get { return (ImageSource)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(ImageSource), typeof(CardMenu), new PropertyMetadata(default));

    /// <summary>
    /// 卡片标题
    /// </summary>
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(CardMenu), new PropertyMetadata(default));

    /// <summary>
    /// 卡片描述
    /// </summary>
    public string Description
    {
        get { return (string)GetValue(DescriptionProperty); }
        set { SetValue(DescriptionProperty, value); }
    }
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register("Description", typeof(string), typeof(CardMenu), new PropertyMetadata(default));

    /// <summary>
    /// 卡片内容
    /// </summary>
    public UIElement CardContent
    {
        get { return (UIElement)GetValue(CardContentProperty); }
        set { SetValue(CardContentProperty, value); }
    }
    public static readonly DependencyProperty CardContentProperty =
        DependencyProperty.Register("CardContent", typeof(UIElement), typeof(CardMenu), new PropertyMetadata(default));
}
