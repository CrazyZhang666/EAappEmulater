namespace EAappEmulater.Api;

public class Friends
{
    public List<EntriesItem> entries { get; set; }
    public PagingInfo pagingInfo { get; set; }
}

public class EntriesItem
{
    public long timestamp { get; set; }
    public string friendType { get; set; }
    public string dateTime { get; set; }
    public long userId { get; set; }
    public bool favorite { get; set; }
    [JsonIgnore]
    public object edgeAttribute { get; set; }
    public string userType { get; set; }
    public string displayName { get; set; }
    public long personaId { get; set; }
    public string nickName { get; set; }
    public string _friendType { get; set; }
}

public class PagingInfo
{
    public int size { get; set; }
    public int offset { get; set; }
    public int totalSize { get; set; }
}
