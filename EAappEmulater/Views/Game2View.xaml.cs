using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Models;
using EAappEmulater.Windows;

namespace EAappEmulater.Views;

/// <summary>
/// Game2View.xaml 的交互逻辑
/// </summary>
public partial class Game2View : UserControl
{
    public ObservableCollection<GameMenuInfo> ObsCol_GameMenuInfos { get; set; } = new();

    public Game2View()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        foreach (var item in Base.GameInfoDb)
        {
            ObsCol_GameMenuInfos.Add(new()
            {
                GameType = item.Value.GameType,
                Image = item.Value.Image,
                IsInstalled = item.Value.IsInstalled,
                RunGameCommand = RunGameCommand,
                SetGameOptionCommand = SetGameOptionCommand
            });
        }
    }

    [RelayCommand]
    private void RunGame(GameType gameType)
    {
        Game.RunGame(gameType);
    }

    [RelayCommand]
    private void SetGameOption(GameType gameType)
    {
        var advancedWindow = new AdvancedWindow(gameType)
        {
            Owner = MainWindow.MainWinInstance
        };

        MainWindow.MainWinInstance.IsShowMaskLayer = true;
        advancedWindow.ShowDialog();
        MainWindow.MainWinInstance.IsShowMaskLayer = false;
    }
}
