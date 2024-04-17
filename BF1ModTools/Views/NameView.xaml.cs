using BF1ModTools.Core;
using BF1ModTools.Utils;
using BF1ModTools.Helper;
using CommunityToolkit.Mvvm.Input;

namespace BF1ModTools.Views;

/// <summary>
/// NameView.xaml 的交互逻辑
/// </summary>
public partial class NameView : UserControl
{
    /// <summary>
    /// playername.txt 文件路径
    /// </summary>
    private string File_PlayerName;

    /// <summary>
    /// UTF8编码无BOM
    /// </summary>
    private readonly UTF8Encoding UTF8EncodingNoBom = new(false);

    public NameView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        TextBlock_PlayerName.Text = Account.PlayerName;
        TextBlock_PersonaId.Text = Account.PersonaId;

        ///////////////////////////////////////////

        if (string.IsNullOrWhiteSpace(Globals.BF1InstallDir))
            return;

        File_PlayerName = Path.Combine(Globals.BF1InstallDir, "playername.txt");
        if (!File.Exists(File_PlayerName))
            return;

        TextBox_CustomName.Text = File.ReadAllText(File_PlayerName, UTF8EncodingNoBom);
    }

    #region 自定义区域
    [RelayCommand]
    private void ChangePlayerName()
    {
        if (ProcessHelper.IsAppRun(CoreUtil.Name_BF1))
        {
            NotifierHelper.Warning("战地1正在运行，请关闭后再执行修改ID操作");
            return;
        }

        var playerName = TextBox_CustomName.Text.Trim();
        if (string.IsNullOrWhiteSpace(playerName))
        {
            NotifierHelper.Warning("游戏ID不能为空，请重新修改");
            return;
        }

        var nameHexBytes = Encoding.UTF8.GetBytes(playerName);
        if (nameHexBytes.Length > 15)
        {
            NotifierHelper.Warning("游戏ID字节数不能超过15字节，请重新修改");
            return;
        }

        try
        {
            File.WriteAllText(File_PlayerName, playerName, UTF8EncodingNoBom);
            NotifierHelper.Success("修改中文游戏ID成功，请启动战地1在线模式生效");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("修改中文游戏ID发生异常", ex);
            NotifierHelper.Error("修改中文游戏ID发生异常");
        }
    }
    #endregion
}
