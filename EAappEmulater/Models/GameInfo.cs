using EAappEmulater.Enums;

namespace EAappEmulater.Models;

public class GameInfo
{
    public GameType GameType { get; set; }

    public string Name { get; set; }
    public string Name2 { get; set; }
    public string Image { get; set; }
    public string Image2 { get; set; }

    public bool IsUseCustom { get; set; }

    public string Dir { get; set; }
    public string Dir2 { get; set; }
    public string Args { get; set; }
    public string Args2 { get; set; }

    public bool IsInstalled { get; set; }

    public bool IsEAAC { get; set; }
    public string AppName { get; set; }
    public string ContentId { get; set; }

    public string Regedit { get; set; }
    public string Regedit2 { get; set; }

    public List<string> Locales { get; set; }

    public bool IsOldLSX { get; set; }
}
