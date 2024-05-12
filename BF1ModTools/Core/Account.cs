using BF1ModTools.Utils;
using BF1ModTools.Helper;

namespace BF1ModTools.Core;

public static class Account
{
    private static readonly string _iniPath;

    ///////////////////////////////////

    public static string PlayerName { get; set; }
    public static string PersonaId { get; set; }
    public static string UserId { get; set; }

    ///////////////////////////////////

    public static string Remid { get; set; }
    public static string Sid { get; set; }
    public static string AccessToken { get; set; }
    public static string OriginPCAuth { get; set; }
    public static string OriginPCToken { get; set; }
    public static string LSXAuthCode { get; set; }

    ///////////////////////////////////

    static Account()
    {
        _iniPath = Path.Combine(CoreUtil.Dir_Config, "Account.ini");
    }

    /// <summary>
    /// 重置配置文件
    /// </summary>
    public static void Reset()
    {
        PlayerName = string.Empty;
        PersonaId = string.Empty;
        UserId = string.Empty;

        Remid = string.Empty;
        Sid = string.Empty;
        AccessToken = string.Empty;
        OriginPCAuth = string.Empty;
        OriginPCToken = string.Empty;
        LSXAuthCode = string.Empty;
    }

    /// <summary>
    /// 读取配置文件
    /// </summary>
    public static void Read()
    {
        PlayerName = ReadString("Account", "PlayerName");
        PersonaId = ReadString("Account", "PersonaId");
        UserId = ReadString("Account", "UserId");

        Remid = ReadString("Cookie", "Remid");
        Sid = ReadString("Cookie", "Sid");
        AccessToken = ReadString("Cookie", "AccessToken");
        OriginPCAuth = ReadString("Cookie", "OriginPCAuth");
        OriginPCToken = ReadString("Cookie", "OriginPCToken");
        LSXAuthCode = ReadString("Cookie", "LSXAuthCode");
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    public static void Write()
    {
        WriteString("Account", "PlayerName", PlayerName);
        WriteString("Account", "PersonaId", PersonaId);
        WriteString("Account", "UserId", UserId);

        WriteString("Cookie", "Remid", Remid);
        WriteString("Cookie", "Sid", Sid);
        WriteString("Cookie", "AccessToken", AccessToken);
        WriteString("Cookie", "OriginPCAuth", OriginPCAuth);
        WriteString("Cookie", "OriginPCToken", OriginPCToken);
        WriteString("Cookie", "LSXAuthCode", LSXAuthCode);
    }

    private static string ReadString(string section, string key)
    {
        return IniHelper.ReadString(section, key, _iniPath);
    }

    private static void WriteString(string section, string key, string value)
    {
        IniHelper.WriteString(section, key, value, _iniPath);
    }
}
