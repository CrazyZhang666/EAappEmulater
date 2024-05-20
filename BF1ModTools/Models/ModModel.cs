using CommunityToolkit.Mvvm.ComponentModel;

namespace BF1ModTools.Models;

public partial class ModModel : ObservableObject
{
    [ObservableProperty]
    private string modName;

    [ObservableProperty]
    private string modDate;

    [ObservableProperty]
    private string modMD5;

    [ObservableProperty]
    private string modPath;

    [ObservableProperty]
    private string checkState;

    [ObservableProperty]
    private bool isNeedSelect;

    [ObservableProperty]
    private bool isCanRunGame;
}
