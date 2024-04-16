namespace ModernWpf.Controls;

[TemplatePart(Name = "PART_Border", Type = typeof(Border))]
public class Dialog : Control
{
    /// <summary>
    /// 对话框状态
    /// </summary>
    public bool IsOpen
    {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register("IsOpen", typeof(bool), typeof(Dialog), new PropertyMetadata(false));

    /// <summary>
    /// 对话框内容
    /// </summary>
    public UIElement Content
    {
        get { return (UIElement)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(UIElement), typeof(Dialog), new PropertyMetadata(default));

    private Border _border;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _border = GetTemplateChild("PART_Border") as Border;
        _border.MouseLeftButtonDown += (s, e) => { IsOpen = false; };
    }
}
