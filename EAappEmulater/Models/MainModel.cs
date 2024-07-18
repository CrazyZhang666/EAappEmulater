using CommunityToolkit.Mvvm.ComponentModel;

namespace EAappEmulater.Models;

public partial class MainModel : ObservableObject
{
    [ObservableProperty]
    private string avatar;

    [ObservableProperty]
    private string playerName;

    [ObservableProperty]
    private string personaId;
}
