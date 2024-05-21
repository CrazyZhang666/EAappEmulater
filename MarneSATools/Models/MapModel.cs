using CommunityToolkit.Mvvm.ComponentModel;

namespace MarneSATools.Models;

public partial class MapModel : ObservableObject
{
    [ObservableProperty]
    private int index;

    [ObservableProperty]
    private string dlc;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string url;

    [ObservableProperty]
    private string mode;

    [ObservableProperty]
    private string code;
}
