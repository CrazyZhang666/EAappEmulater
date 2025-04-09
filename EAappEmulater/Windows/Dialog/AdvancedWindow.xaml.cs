using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Models;

namespace EAappEmulater.Windows;

/// <summary>
/// AdvancedWindow.xaml 的交互逻辑
/// </summary>
public partial class AdvancedWindow
{
    public AdvancedModel AdvancedModel { get; set; } = new();
    public List<LocaleInfo> LocaleInfos { get; set; } = new();

    private readonly GameInfo _gameInfo;

    private bool _isGetRegeditSuccess;

    public AdvancedWindow(GameType gameType)
    {
        InitializeComponent();

        _gameInfo = Base.GameInfoDb[gameType];

        if (Globals.Language == "zh-CN")
        {
            AdvancedModel.Name = _gameInfo.Name;
        } else
        {
            AdvancedModel.Name = _gameInfo.Name2;
        }
        AdvancedModel.Name2 = _gameInfo.Name2;
        AdvancedModel.Image = _gameInfo.Image;

        AdvancedModel.IsUseCustom = _gameInfo.IsUseCustom;

        AdvancedModel.GameDir = _gameInfo.Dir;
        AdvancedModel.GameArgs = _gameInfo.Args;
        AdvancedModel.GameDir2 = _gameInfo.Dir2;
        AdvancedModel.GameArgs2 = _gameInfo.Args2;

        /////////////////////////////////////////////////

        LocaleInfos.Add(Base.GameLocaleDb.First().Value);

        foreach (var locale in _gameInfo.Locales)
        {
            if (Base.GameLocaleDb.TryGetValue(locale, out LocaleInfo info))
            {
                LocaleInfos.Add(info);
            }
            else
            {
                LocaleInfos.Add(new()
                {
                    Code = locale
                });
            }
        }
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private void Window_Advanced_Loaded(object sender, RoutedEventArgs e)
    {
        GetGameLocale();
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    private void Window_Advanced_Closing(object sender, CancelEventArgs e)
    {
    }

    /// <summary>
    /// 选择文件
    /// </summary>
    [RelayCommand]
    private void SelcetFilePath()
    {
        var dialog = new OpenFileDialog
        {
            Title = I18nHelper.I18n._("Windows.AdvancedWindow.FileDialogTitle"),
            FileName = _gameInfo.AppName,
            DefaultExt = ".exe",
            Filter = "可执行文件 (.exe)|*.exe",
            Multiselect = false,
            RestoreDirectory = true,
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            AdvancedModel.GameDir2 = Path.GetDirectoryName(dialog.FileName);
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private void SaveOption()
    {
        Base.GameInfoDb[_gameInfo.GameType].IsUseCustom = AdvancedModel.IsUseCustom;

        // GameDir 注册表获取，禁止修改
        Base.GameInfoDb[_gameInfo.GameType].Args = AdvancedModel.GameArgs;

        Base.GameInfoDb[_gameInfo.GameType].Dir2 = AdvancedModel.GameDir2;
        Base.GameInfoDb[_gameInfo.GameType].Args2 = AdvancedModel.GameArgs2;

        SetGameLocale();

        this.Close();
    }

    /// <summary>
    /// 取消设置
    /// </summary>
    [RelayCommand]
    private void CancelOption()
    {
        this.Close();
    }

    /// <summary>
    /// 获取注册表游戏语言信息
    /// </summary>
    private void GetGameLocale()
    {
        var locale = RegistryHelper.GetRegistryLocale(_gameInfo.Regedit);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var index = LocaleInfos.FindIndex(x => x.Code == locale);
            ComboBox_LocaleInfos.SelectedIndex = index == -1 ? 0 : index;

            _isGetRegeditSuccess = true;
            return;
        }

        locale = RegistryHelper.GetRegistryLocale(_gameInfo.Regedit2);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var index = LocaleInfos.FindIndex(x => x.Code == locale);
            ComboBox_LocaleInfos.SelectedIndex = index == -1 ? 0 : index;

            _isGetRegeditSuccess = true;
            return;
        }

        ComboBox_LocaleInfos.SelectedIndex = 0;
    }

    /// <summary>
    /// 设置注册表游戏语言信息
    /// </summary>
    private void SetGameLocale()
    {
        if (!_isGetRegeditSuccess)
            return;

        if (string.IsNullOrWhiteSpace(AdvancedModel.GameDir))
            return;

        if (!Directory.Exists(AdvancedModel.GameDir))
            return;

        if (ComboBox_LocaleInfos.SelectedItem is not LocaleInfo item || item.Code == "NULL")
            return;

        RegistryHelper.SetRegistryTargetVaule(_gameInfo.Regedit, "Locale", item.Code);
        RegistryHelper.SetRegistryTargetVaule(_gameInfo.Regedit2, "Locale", item.Code);
    }

    /// <summary>
    /// 按住鼠标左键移动窗口
    /// </summary>
    private void Image_Game_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}
