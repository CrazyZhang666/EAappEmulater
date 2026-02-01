using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Helper;
using EAappEmulater.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EAappEmulater.Views;

/// <summary>
/// SettingView.xaml 的交互逻辑
/// </summary>
public partial class SettingView : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _closeToTray = true;
    public bool CloseToTray
    {
        get => _closeToTray;
        set
        {
            if (_closeToTray == value) return;
            _closeToTray = value;
            OnPropertyChanged(nameof(CloseToTray));

            // 同步到全局配置
            Globals.CloseToTray = value;
            Globals.Write();
        }
    }

    private ObservableCollection<LanguageEntry> _languageList = new();
    public ObservableCollection<LanguageEntry> LanguageList
    {
        get => _languageList;
        set { _languageList = value; OnPropertyChanged(nameof(LanguageList)); }
    }

    private string _currentLanguage = string.Empty;
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value) return;
            _currentLanguage = value;
            OnPropertyChanged(nameof(CurrentLanguage));

            // Apply immediately when changed by the ComboBox
            if (!string.IsNullOrWhiteSpace(_currentLanguage))
            {
                try
                {
                    App.SetLanguage(_currentLanguage);
                    Globals.Language = _currentLanguage;
                    Globals.DefaultLanguage = _currentLanguage;
                    Globals.Write();
                }
                catch { }
            }
        }
    }

    public SettingView()
    {
        InitializeComponent();

        ToDoList();

        // load languages
        var langs = LanguageConfigHelper.GetLanguages();
        LanguageList = new ObservableCollection<LanguageEntry>(langs);

        // show current language
        CurrentLanguage = string.IsNullOrWhiteSpace(Globals.Language) ? (Globals.DefaultLanguage ?? "") : Globals.Language;
        if (string.IsNullOrWhiteSpace(CurrentLanguage) && LanguageList.Count > 0)
            CurrentLanguage = LanguageList[0].Code;

        // 初始化关闭窗口行为设置
        CloseToTray = Globals.CloseToTray;

        // 设置 ComboBox 默认选中项
        foreach (ComboBoxItem item in ComboBox_CloseBehavior.Items)
        {
            if (item.Tag?.ToString() == CloseToTray.ToString())
            {
                ComboBox_CloseBehavior.SelectedItem = item;
                break;
            }
        }

        DataContext = this;
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// 关闭窗口行为 ComboBox 选择改变事件
    /// </summary>
    private void ComboBox_CloseBehavior_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboBox_CloseBehavior.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
        {
            if (bool.TryParse(selectedItem.Tag.ToString(), out bool closeToTray))
            {
                CloseToTray = closeToTray;
            }
        }
    }

    private void ToDoList()
    {
        FormLabel_VersionInfo.Content = CoreUtil.VersionInfo.ToString();

        FormLabel_UserName.Content = CoreUtil.UserName;
        FormLabel_MachineName.Content = CoreUtil.MachineName;
        FormLabel_OSVersion.Content = CoreUtil.OSVersion;
        FormLabel_SystemDirectory.Content = CoreUtil.SystemDirectory;

        FormLabel_RuntimeVersion.Content = CoreUtil.RuntimeVersion;
        FormLabel_OSArchitecture.Content = CoreUtil.OSArchitecture;
        FormLabel_RuntimeIdentifier.Content = CoreUtil.RuntimeIdentifier;
    }

    /// <summary>
    /// 打开配置文件
    /// </summary>
    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(CoreUtil.Dir_Default);
    }


    /// <summary>
    /// 切换语言 (保留为命令入口, 也会使用 CurrentLanguage 的 setter)
    /// </summary>
    [RelayCommand]
    private void ChangeLanguage()
    {
        if (!string.IsNullOrWhiteSpace(CurrentLanguage))
        {
            App.SetLanguage(CurrentLanguage);
            Globals.Language = CurrentLanguage;
            Globals.DefaultLanguage = CurrentLanguage;
            Globals.Write();
        }
    }
}
