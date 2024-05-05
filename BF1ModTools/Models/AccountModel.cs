using CommunityToolkit.Mvvm.ComponentModel;

namespace BF1ModTools.Models;

public partial class AccountModel : ObservableObject
{
    [ObservableProperty]
    private string playerName;

    [ObservableProperty]
    private string remid;

    [ObservableProperty]
    private string sid;
}
