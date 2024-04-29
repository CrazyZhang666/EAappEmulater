namespace ModernWpf.Controls;

[TemplatePart(Name = "PART_ButtonRunGame", Type = typeof(Button))]
[TemplatePart(Name = "PART_ButtonSetGameArgs", Type = typeof(Button))]
public class GameMenu : Control
{
    /// <summary>
    /// 游戏封面
    /// </summary>
    public string Image
    {
        get { return (string)GetValue(ImageProperty); }
        set { SetValue(ImageProperty, value); }
    }
    public static readonly DependencyProperty ImageProperty =
        DependencyProperty.Register("Image", typeof(string), typeof(GameMenu), new PropertyMetadata(default));

    /// <summary>
    /// 游戏是否安装
    /// </summary>
    public bool IsInstalled
    {
        get { return (bool)GetValue(IsInstalledProperty); }
        set { SetValue(IsInstalledProperty, value); }
    }
    public static readonly DependencyProperty IsInstalledProperty =
        DependencyProperty.Register("IsInstalled", typeof(bool), typeof(GameMenu), new PropertyMetadata(default));

    #region RunGameEvent
    /// <summary>
    /// 注册按钮RunGame自定义事件
    /// </summary>
    public static readonly RoutedEvent RunGameEvent =
        EventManager.RegisterRoutedEvent("RunGameEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GameMenu));
    public event RoutedEventHandler RunGame
    {
        add { AddHandler(RunGameEvent, value); }
        remove { RemoveHandler(RunGameEvent, value); }
    }
    protected void OnRunGameEvent()
    {
        var args = new RoutedEventArgs(RunGameEvent, this);
        this.RaiseEvent(args);
    }

    public ICommand Command
    {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(GameMenu), new PropertyMetadata(default));

    public object CommandParameter
    {
        get { return GetValue(CommandParameterProperty); }
        set { SetValue(CommandParameterProperty, value); }
    }
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register("CommandParameter", typeof(object), typeof(GameMenu), new PropertyMetadata(default));
    #endregion

    #region SetGameOptionEvent
    /// <summary>
    /// 注册按钮SetGameOption自定义事件
    /// </summary>
    public static readonly RoutedEvent SetGameOptionEvent =
        EventManager.RegisterRoutedEvent("SetGameOptionEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GameMenu));
    public event RoutedEventHandler SetGameOption
    {
        add { AddHandler(SetGameOptionEvent, value); }
        remove { RemoveHandler(SetGameOptionEvent, value); }
    }
    protected void OnSetGameOptionEvent()
    {
        var args = new RoutedEventArgs(SetGameOptionEvent, this);
        this.RaiseEvent(args);
    }

    public ICommand Command2
    {
        get { return (ICommand)GetValue(Command2Property); }
        set { SetValue(Command2Property, value); }
    }
    public static readonly DependencyProperty Command2Property =
        DependencyProperty.Register("Command2", typeof(ICommand), typeof(GameMenu), new PropertyMetadata(default));

    public object CommandParameter2
    {
        get { return GetValue(CommandParameterProperty2); }
        set { SetValue(CommandParameterProperty2, value); }
    }
    public static readonly DependencyProperty CommandParameterProperty2 =
        DependencyProperty.Register("CommandParameter2", typeof(object), typeof(GameMenu), new PropertyMetadata(default));
    #endregion

    private Button _buttonRunGame;
    private Button _buttonSetGameArgs;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _buttonRunGame = GetTemplateChild("PART_ButtonRunGame") as Button;
        _buttonSetGameArgs = GetTemplateChild("PART_ButtonSetGameArgs") as Button;

        _buttonRunGame.Click += (s, e) =>
        {
            OnRunGameEvent();
            Command?.Execute(CommandParameter);
        };

        _buttonSetGameArgs.Click += (s, e) =>
        {
            OnSetGameOptionEvent();
            Command2?.Execute(CommandParameter2);
        };
    }
}
