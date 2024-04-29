using EAappEmulater.Enums;

namespace EAappEmulater.Models;

public class AccountInfo
{
    public AccountSlot AccountSlot { get; set; }

    public string PlayerName { get; set; }
    public string AvatarId { get; set; }
    public string Avatar { get; set; }

    public string Remid { get; set; }
    public string Sid { get; set; }
}
