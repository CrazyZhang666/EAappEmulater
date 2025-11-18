using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModernWpf.Controls;

[TemplatePart(Name = "PART_ButtonDownload", Type = typeof(Button))]
public class OwnedGameMenu : Control
{
    static OwnedGameMenu()
    {
        // 关联默认样式（从 Generic.xaml 合并字典中加载）
        DefaultStyleKeyProperty.OverrideMetadata(typeof(OwnedGameMenu), new FrameworkPropertyMetadata(typeof(OwnedGameMenu)));
    }

    public string GameName
    {
        get => (string)GetValue(GameNameProperty);
        set => SetValue(GameNameProperty, value);
    }
    public static readonly DependencyProperty GameNameProperty =
        DependencyProperty.Register("GameName", typeof(string), typeof(OwnedGameMenu), new PropertyMetadata(string.Empty));

    public string OfferId
    {
        get => (string)GetValue(OfferIdProperty);
        set => SetValue(OfferIdProperty, value);
    }
    public static readonly DependencyProperty OfferIdProperty =
        DependencyProperty.Register("OfferId", typeof(string), typeof(OwnedGameMenu), new PropertyMetadata(string.Empty));

    public string GameTypeText
    {
        get => (string)GetValue(GameTypeTextProperty);
        set => SetValue(GameTypeTextProperty, value);
    }
    public static readonly DependencyProperty GameTypeTextProperty =
        DependencyProperty.Register("GameTypeText", typeof(string), typeof(OwnedGameMenu), new PropertyMetadata(string.Empty));

    public ICommand DownloadCommand
    {
        get => (ICommand)GetValue(DownloadCommandProperty);
        set => SetValue(DownloadCommandProperty, value);
    }
    public static readonly DependencyProperty DownloadCommandProperty =
        DependencyProperty.Register("DownloadCommand", typeof(ICommand), typeof(OwnedGameMenu), new PropertyMetadata(default));

    public object DownloadCommandParameter
    {
        get => GetValue(DownloadCommandParameterProperty);
        set => SetValue(DownloadCommandParameterProperty, value);
    }
    public static readonly DependencyProperty DownloadCommandParameterProperty =
        DependencyProperty.Register("DownloadCommandParameter", typeof(object), typeof(OwnedGameMenu), new PropertyMetadata(default));

    private Button _buttonDownload;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _buttonDownload = GetTemplateChild("PART_ButtonDownload") as Button;
        if (_buttonDownload != null)
        {
            _buttonDownload.Click += (s, e) => { DownloadCommand?.Execute(DownloadCommandParameter); };
        }
    }
}
