namespace EAappEmulater.Models;

public class FriendInfo
{
    public int Index { get; set; }
    public string Avatar { get; set; }
    public int DiffDays { get; set; }

    public string DisplayName { get; set; }
    public string NickName { get; set; }
    public long UserId { get; set; }
    public long PersonaId { get; set; }
    public string FriendType { get; set; }
    public string DateTime { get; set; }
}
