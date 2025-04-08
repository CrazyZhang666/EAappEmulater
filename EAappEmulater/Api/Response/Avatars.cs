using static EAappEmulater.Api.Avatars;

namespace EAappEmulater.Api;

public class Avatars : Dictionary<string, UserAvatar>
{
    public class UserAvatar
    {
        public Avatar avatar { get; set; }
    }

    public class Avatar
    {
        public int avatarId { get; set; }
        public AvatarSize large { get; set; }
    }

    public class AvatarSize
    {
        public string path { get; set; }
    }
}

public class AvatarInfo
{
    public int AvatarId { get; set; }
    public string Url { get; set; }
}


public class UsersItem
{
    public long userId { get; set; }
    public Avatar avatar { get; set; }
}

public class Avatar
{
    public int avatarId { get; set; }
    [JsonIgnore]
    public string orderNumber { get; set; }
    [JsonIgnore]
    public string isRecent { get; set; }
    public string link { get; set; }
    [JsonIgnore]
    public string typeId { get; set; }
    [JsonIgnore]
    public string typeName { get; set; }
    [JsonIgnore]
    public string statusId { get; set; }
    [JsonIgnore]
    public string statusName { get; set; }
    [JsonIgnore]
    public string galleryId { get; set; }
    [JsonIgnore]
    public string galleryName { get; set; }
}