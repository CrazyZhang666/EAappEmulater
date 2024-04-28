using CommunityToolkit.Mvvm.ComponentModel;

namespace EAappEmulater.Models;

public partial class AccountModel : ObservableObject
{
    [ObservableProperty]
    private string avatarId;

    [ObservableProperty]
    private string avatar;

    [ObservableProperty]
    private string playerName;

    [ObservableProperty]
    private string personaId;

    [ObservableProperty]
    private string userId;

    [ObservableProperty]
    private string remid;

    [ObservableProperty]
    private string sid;

    [ObservableProperty]
    private string token;
}
