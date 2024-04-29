namespace ModernWpf.Controls;

[TemplatePart(Name = "PART_Hyperlink", Type = typeof(Hyperlink))]
public class IconHyperlink : Control
{
    /// <summary>
    /// 超链接图标
    /// </summary>
    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(string), typeof(IconHyperlink), new PropertyMetadata(default));

    /// <summary>
    /// 超链接文本
    /// </summary>
    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(IconHyperlink), new PropertyMetadata(default));

    /// <summary>
    /// 超链接Url
    /// </summary>
    public Uri Uri
    {
        get { return (Uri)GetValue(UriProperty); }
        set { SetValue(UriProperty, value); }
    }
    public static readonly DependencyProperty UriProperty =
        DependencyProperty.Register("Uri", typeof(Uri), typeof(IconHyperlink), new PropertyMetadata(default));

    private Hyperlink _hyperlink;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _hyperlink = GetTemplateChild("PART_Hyperlink") as Hyperlink;
        _hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var link = e.Uri.AbsoluteUri;

        if (!link.StartsWith("http"))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }
        catch { }
    }
}
