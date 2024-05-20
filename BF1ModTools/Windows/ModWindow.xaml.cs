using BF1ModTools.Api;
using BF1ModTools.Utils;
using BF1ModTools.Helper;
using BF1ModTools.Models;
using CommunityToolkit.Mvvm.Input;

namespace BF1ModTools.Windows;

/// <summary>
/// ModWindow.xaml 的交互逻辑
/// </summary>
public partial class ModWindow
{
    public ModModel ModModel { get; set; } = new();

    public ModWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private async void Window_Mods_Loaded(object sender, RoutedEventArgs e)
    {
        var modInfo = await CoreApi.GetWebModInfo();
        if (string.IsNullOrWhiteSpace(modInfo))
        {
            ModModel.ModName = "获取服务器Mod信息失败";
            ModModel.ModDate = "获取服务器Mod信息失败";
            ModModel.ModMD5 = "获取服务器Mod信息失败";

            ModModel.CheckState = "获取服务器Mod信息失败，请手动选择Mod文件";
            ModModel.IsNeedSelect = true;
            ModModel.IsCanRunGame = false;

            return;
        }

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(modInfo);

            var root = xmlDoc.DocumentElement;
            var webMd5 = root.SelectSingleNode("MD5")?.InnerText;

            // 校验MD5是否合法
            if (!Regex.IsMatch(webMd5, "^[a-fA-F0-9]{32}$"))
            {
                ModModel.ModName = "解析服务器Mod信息失败";
                ModModel.ModDate = "解析服务器Mod信息失败";
                ModModel.ModMD5 = "解析服务器Mod信息失败";

                ModModel.CheckState = "解析服务器Mod信息失败，请手动选择Mod文件";
                ModModel.IsNeedSelect = true;
                ModModel.IsCanRunGame = false;

                return;
            }

            ModModel.ModName = root.SelectSingleNode("Name")?.InnerText;
            ModModel.ModDate = root.SelectSingleNode("Date")?.InnerText;
            ModModel.ModMD5 = webMd5;

            var modPath = Path.Combine(CoreUtil.Dir_Mods_Bf1, ModModel.ModName);
            var localMd5 = await FileHelper.GetFileMD5(modPath);
            // 比对本地Md5值
            if (localMd5 != webMd5)
            {
                ModModel.IsNeedSelect = true;
                ModModel.IsCanRunGame = false;
                ModModel.CheckState = "检测到本地Mod文件与服务器不一致，请重新选择";
                return;
            }

            ModModel.ModPath = modPath;
            ModModel.CheckState = "恭喜，本地Mod文件与服务器一致，现在可以启动游戏";

            ModModel.IsNeedSelect = false;
            ModModel.IsCanRunGame = true;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("解析Mod信息发生异常", ex);

            ModModel.ModName = "解析服务器Mod信息异常";
            ModModel.ModDate = "解析服务器Mod信息异常";
            ModModel.ModMD5 = "解析服务器Mod信息异常";

            ModModel.CheckState = "解析服务器Mod信息异常，请手动选择Mod文件";
            ModModel.IsNeedSelect = true;
            ModModel.IsCanRunGame = false;
        }
    }

    /// <summary>
    /// 窗口关闭时事件
    /// </summary>
    private void Window_Mods_Closing(object sender, CancelEventArgs e)
    {
    }

    /// <summary>
    /// 选择Mod文件路径
    /// </summary>
    [RelayCommand]
    private async Task SelcetModPath()
    {
        // 选择要安装的Mod文件
        var dialog = new OpenFileDialog
        {
            Title = "选择要安装的寒霜Mod文件",
            DefaultExt = ".fbmod",
            Filter = "寒霜Mod文件 (.fbmod)|*.fbmod",
            Multiselect = false,
            InitialDirectory = Globals.DialogDir2,
            RestoreDirectory = true,
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        // 尝试获取web模组名称
        if (!string.IsNullOrWhiteSpace(ModModel.ModName))
        {
            dialog.FileName = ModModel.ModName;
            dialog.Title = $"{dialog.Title}（{ModModel.ModName}）";
        }

        // 如果未选择，则退出程序
        if (dialog.ShowDialog() == false)
            return;

        // 记住本次选择的文件路径
        Globals.DialogDir2 = Path.GetDirectoryName(dialog.FileName);

        /////////////////////////////////////////////////////////

        // 校验选中的mod文件
        if (Regex.IsMatch(ModModel.ModMD5, "^[a-fA-F0-9]{32}$"))
        {
            var localMd5 = await FileHelper.GetFileMD5(dialog.FileName);
            // 比对选择mod文件的Md5值
            if (localMd5 != ModModel.ModMD5)
            {
                ModModel.CheckState = "当前选择的Mod文件与服务器不一致，请重新选择";
                ModModel.IsNeedSelect = true;
                ModModel.IsCanRunGame = false;

                return;
            }
        }

        /////////////////////////////////////////////////////////

        try
        {
            // Mod文件夹如果不存在则创建
            FileHelper.CreateDirectory(CoreUtil.Dir_Mods_Bf1);

            // 清空Mod文件夹全部文件
            FileHelper.ClearDirectory(CoreUtil.Dir_Mods_Bf1);
            LoggerHelper.Info("清空 Mod文件夹 旧版文件成功");

            // 获取文件名称，带扩展名
            var fileName = Path.GetFileName(dialog.FileName);
            // 复制mod文件到 寒霜mod管理器 指定mod文件夹
            File.Copy(dialog.FileName, Path.Combine(CoreUtil.Dir_Mods_Bf1, fileName), true);

            ModModel.ModPath = dialog.FileName;
            LoggerHelper.Info($"已选择战地1 Mod 名称: {fileName}");

            ModModel.CheckState = "恭喜，当前选择Mod文件与服务器一致，现在可以启动游戏";
            ModModel.IsNeedSelect = false;
            ModModel.IsCanRunGame = true;
        }
        catch (Exception ex)
        {
            NotifierHelper.Error("选择 Mod 过程中发生异常");
            LoggerHelper.Error($"选择 Mod 过程中发生异常 {ex.Message}");
        }
    }

    /// <summary>
    /// 运行战地1Mod游戏
    /// </summary>
    [RelayCommand]
    private void RunBf1ModGame()
    {
        try
        {
            // 获取文件名称，带扩展名
            var fileName = Path.GetFileName(ModModel.ModPath);

            // 创建FrostyMod配置文件
            var modConfig = new ModConfig();

            // 设置战地1安装目录
            modConfig.Games.bf1.GamePath = Globals.BF1InstallDir;

            // 设置Mod名称并启用
            modConfig.Games.bf1.Packs.Marne = $"{fileName}:True";

            // 写入 Frosty\manager_config.json 配置文件
            File.WriteAllText(CoreUtil.File_Config_ManagerConfig, JsonHelper.JsonSerialize(modConfig));
            LoggerHelper.Info("写入 FrostyModManager 配置文件成功");

            LoggerHelper.Info("正在启动 FrostyModManager 中...");
            ProcessHelper.OpenProcess(CoreUtil.File_FrostyMod_FrostyModManager);

            this.Close();
        }
        catch (Exception ex)
        {
            NotifierHelper.Error("安装 Mod 过程中发生异常");
            LoggerHelper.Error($"安装 Mod 过程中发生异常 {ex.Message}");
        }
    }
}
