using MarneSATools.Core;
using MarneSATools.Models;
using CommunityToolkit.Mvvm.Input;

namespace MarneSATools;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow
{
    public ObservableCollection<ModeInfo> GameModeInfos { get; set; } = new();
    public ObservableCollection<Map2Info> GameMap2Infos { get; set; } = new();
    public ObservableCollection<MapModel> GameMapModels { get; set; } = new();

    private const string _mapListPath = ".\\MapList.txt";

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

        GameMapModels.CollectionChanged += (s, e) => { ReSortMapIndex(); };

        if (File.Exists(_mapListPath))
        {
            var allLines = File.ReadAllLines(_mapListPath);

            int index = 0;
            foreach (var line in allLines)
            {
                var array = line.Split(';');
                if (array.Length != 3)
                    continue;

                var mapInfo = Base.GameMapInfoDb.Find(x => x.Url == array[0]);
                if (mapInfo == null)
                    continue;

                var modeInfo = Base.GameModeInfoDb.Find(x => x.Code == array[1]);
                if (modeInfo == null)
                    continue;

                var mapModel = new MapModel
                {
                    Index = ++index,
                    Dlc = mapInfo.DLC,
                    Name = mapInfo.Name,
                    Url = mapInfo.Url,
                    Mode = modeInfo.Name,
                    Code = modeInfo.Code
                };

                GameMapModels.Add(mapModel);
            }
        }
    }

    private void Window_Main_Closing(object sender, CancelEventArgs e)
    {
        SaveData();
    }

    private void ReSortMapIndex()
    {
        int index = 0;
        foreach (var item in GameMapModels)
        {
            item.Index = ++index;
        }
    }

    private void ListBox_GameModeInfos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBox_GameModeInfos.SelectedItem is null)
            return;

        GameMap2Infos.Clear();

        var modeInfo = ListBox_GameModeInfos.SelectedItem as ModeInfo;

        var index = 0;
        var result = Base.GameMapInfoDb.Where(x => x.Modes.Contains(modeInfo.Code));
        foreach (var item in result)
        {
            GameMap2Infos.Add(new()
            {
                Index = ++index,
                DLC = item.DLC,
                Name = item.Name,
                Url = item.Url
            });
        }
        ListBox_GameMap2Infos.SelectedIndex = 0;
    }

    [RelayCommand]
    private void AddMap()
    {
        if (ListBox_GameModeInfos.SelectedItem is null)
            return;
        if (ListBox_GameMap2Infos.SelectedItem is null)
            return;

        var modeInfo = ListBox_GameModeInfos.SelectedItem as ModeInfo;
        var map2Info = ListBox_GameMap2Infos.SelectedItem as Map2Info;

        GameMapModels.Add(new()
        {
            Dlc = map2Info.DLC,
            Name = map2Info.Name,
            Url = map2Info.Url,
            Mode = modeInfo.Name,
            Code = modeInfo.Code,
        });
    }

    [RelayCommand]
    private void DelectMap()
    {
        if (ListBox_GameMapModels.SelectedItem is null)
            return;

        var mapModel = ListBox_GameMapModels.SelectedItem as MapModel;
        GameMapModels.Remove(mapModel);
    }

    [RelayCommand]
    private void ClearMapList()
    {
        GameMapModels.Clear();
    }

    [RelayCommand]
    private void SaveData()
    {
        var strBuilder = new StringBuilder();
        foreach (var item in GameMapModels)
        {
            strBuilder.AppendLine($"{item.Url};{item.Code};1");
        }

        File.WriteAllText(_mapListPath, strBuilder.ToString());
    }
}