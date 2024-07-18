using CommunityToolkit.Mvvm.ComponentModel;

namespace EAappEmulater.Models;

public partial class AdvancedModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string name2;

    [ObservableProperty]
    private string image;

    [ObservableProperty]
    private bool isUseCustom;

    [ObservableProperty]
    private string gameDir;

    [ObservableProperty]
    private string gameArgs;

    [ObservableProperty]
    private string gameDir2;

    [ObservableProperty]
    private string gameArgs2;

    [ObservableProperty]
    private string gameLoc;
}
