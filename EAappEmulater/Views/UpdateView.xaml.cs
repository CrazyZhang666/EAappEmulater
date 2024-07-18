using EAappEmulater.Helper;

namespace EAappEmulater.Views;

/// <summary>
/// UpdateView.xaml 的交互逻辑
/// </summary>
public partial class UpdateView : UserControl
{
    public UpdateView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        TextBoxHint_UpdateNotes.Text = FileHelper.GetEmbeddedResourceText("Misc.UpdateNotes.txt");
    }
}
