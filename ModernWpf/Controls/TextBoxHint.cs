namespace ModernWpf.Controls;

public class TextBoxHint : TextBox
{
    /// <summary>
    /// Icon图标
    /// </summary>
    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(string), typeof(TextBoxHint), new PropertyMetadata(default));

    /// <summary>
    /// 提示信息
    /// </summary>
    public string Hint
    {
        get { return (string)GetValue(HintProperty); }
        set { SetValue(HintProperty, value); }
    }
    public static readonly DependencyProperty HintProperty =
        DependencyProperty.Register("Hint", typeof(string), typeof(TextBoxHint), new PropertyMetadata("请输入文本"));
}