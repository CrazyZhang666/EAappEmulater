using EAappEmulater.Enums;

namespace EAappEmulater.Models;

public class GameMenuInfo
{
    public GameType GameType { get; set; }
    public string Image { get; set; }
    public bool IsInstalled { get; set; }

    public ICommand RunGameCommand { get; set; }
    public ICommand SetGameOptionCommand { get; set; }
}
