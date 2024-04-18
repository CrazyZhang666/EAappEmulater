using EAappEmulater.Helper;

namespace EAappEmulater.Views;

/// <summary>
/// AboutView.xaml 的交互逻辑
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {

    }

    /// <summary>
    /// 超链接请求导航事件
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        ProcessHelper.OpenLink(e.Uri.OriginalString);
        e.Handled = true;
    }
}
