namespace ModernWpf.Controls;

public class Window : System.Windows.Window
{
    /// <summary>
    /// 标题栏高度
    /// </summary>
    public double CaptionHeight
    {
        get { return (double)GetValue(CaptionHeightProperty); }
        set { SetValue(CaptionHeightProperty, value); }
    }
    public static readonly DependencyProperty CaptionHeightProperty =
        DependencyProperty.Register("CaptionHeight", typeof(double), typeof(Window), new PropertyMetadata(32.0));

    /// <summary>
    /// 标题栏背景色
    /// </summary>
    public Brush CaptionBackground
    {
        get { return (Brush)GetValue(CaptionBackgroundProperty); }
        set { SetValue(CaptionBackgroundProperty, value); }
    }
    public static readonly DependencyProperty CaptionBackgroundProperty =
        DependencyProperty.Register("CaptionBackground", typeof(Brush), typeof(Window), new PropertyMetadata(default));

    /// <summary>
    /// 标题栏内容
    /// </summary>
    public UIElement TitleContent
    {
        get { return (UIElement)GetValue(TitleContentProperty); }
        set { SetValue(TitleContentProperty, value); }
    }
    public static readonly DependencyProperty TitleContentProperty =
        DependencyProperty.Register("TitleContent", typeof(UIElement), typeof(Window), new PropertyMetadata(default));

    /// <summary>
    /// 是否显示遮罩层
    /// </summary>
    public bool IsShowMaskLayer
    {
        get { return (bool)GetValue(IsShowMaskLayerProperty); }
        set { SetValue(IsShowMaskLayerProperty, value); }
    }
    public static readonly DependencyProperty IsShowMaskLayerProperty =
        DependencyProperty.Register("IsShowMaskLayer", typeof(bool), typeof(Window), new PropertyMetadata(false));

    /// <summary>
    /// 窗口构造方法
    /// </summary>
    public Window()
    {
        // 窗口样式
        var chrome = new WindowChrome
        {
            GlassFrameThickness = new Thickness(1),
            CornerRadius = new CornerRadius(0),
            ResizeBorderThickness = new Thickness(4)
        };
        WindowChrome.SetWindowChrome(this, chrome);

        // 将标题栏高度绑定给Chrome
        BindingOperations.SetBinding(chrome, WindowChrome.CaptionHeightProperty,
            new Binding(CaptionHeightProperty.Name) { Source = this });

        // 窗口默认居中
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        // 窗口边框
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F4D4B"));
        BorderThickness = new Thickness(1);

        // 窗口最小化
        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (sender, e) =>
        {
            WindowState = WindowState.Minimized;
        }));

        // 窗口最大化
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (sender, e) =>
        {
            WindowState = WindowState.Maximized;
        }));

        // 窗口还原
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (sender, e) =>
        {
            WindowState = WindowState.Normal;
        }));

        // 窗口关闭
        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (sender, e) =>
        {
            Close();
        }));
    }
}
