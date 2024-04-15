using EAappEmulater.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EAappEmulater.Models;

public partial class AccountInfo : ObservableObject
{
    public int Index { get; set; }
    public bool IsUse { get; set; }
    public AccountSlot AccountSlot { get; set; }

    public string PlayerName { get; set; }
    public string PersonaId { get; set; }
    public string UserId { get; set; }

    [ObservableProperty]
    private string avatar;

    public string Remid { get; set; }
    public string Sid { get; set; }
}
