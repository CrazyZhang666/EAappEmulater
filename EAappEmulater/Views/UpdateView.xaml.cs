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
        if (Globals.Language == "zh-CN")
        {
            TextBoxHint_UpdateNotes.Text = FileHelper.GetEmbeddedResourceText("Misc.UpdateNotes.txt");
        } else
        {
            TextBoxHint_UpdateNotes.Text = FileHelper.GetEmbeddedResourceText("Misc.UpdateNotes_en.txt");
        }
    }
}
