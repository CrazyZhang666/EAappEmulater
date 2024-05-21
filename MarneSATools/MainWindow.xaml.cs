using MarneSATools.Core;
using MarneSATools.Models;

namespace MarneSATools;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow
{
    public ObservableCollection<ModeInfo> GameModeInfos { get; set; } = new();
    public ObservableCollection<string> GameMaps { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Main_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (var item in Base.GameModeInfoDb)
        {
            GameModeInfos.Add(item);
        }
        ListBox_GameModeInfos.SelectedIndex = 0;
    }

    private void Window_Main_Closing(object sender, CancelEventArgs e)
    {

    }

    private void ListBox_GameModeInfos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBox_GameModeInfos.SelectedItem is null)
            return;

        GameMaps.Clear();

        var modeInfo = ListBox_GameModeInfos.SelectedItem as ModeInfo;

        var index = 0;
        var result = Base.GameMapInfoDb.Where(x => x.Modes.Contains(modeInfo.Code));
        foreach (var item in result)
        {
            GameMaps.Add($"[{++index}]  {item.DLC} - {item.Name}");
        }
    }
}