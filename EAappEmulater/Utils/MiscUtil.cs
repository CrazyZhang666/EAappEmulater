namespace EAappEmulater.Utils;

public static class MiscUtil
{
    private static readonly List<string> _gameNameList = new();
    private static readonly List<string> _gameModeList = new();

    static MiscUtil()
    {
        _gameNameList.Add("戰地風雲 3");
        _gameNameList.Add("戰地風雲 4");
        _gameNameList.Add("戰地風雲 硬仗");
        _gameNameList.Add("戰地風雲 1");
        _gameNameList.Add("戰地風雲 V");
        _gameNameList.Add("戰地風雲 2042");

        _gameModeList.Add("征服");
        _gameModeList.Add("突襲");
        _gameModeList.Add("行動");
        _gameModeList.Add("前線");
        _gameModeList.Add("死鬥");
    }

    private static string GetRandomGameName()
    {
        var random = new Random();
        var index = random.Next(_gameNameList.Count - 1);
        return _gameNameList[index];
    }

    private static string GetRandomGamMode()
    {
        var random = new Random();
        var index = random.Next(_gameModeList.Count - 1);
        return _gameModeList[index];
    }

    public static string GetRandomFriendTitle()
    {
        return $"正在遊玩《{GetRandomGameName()}》{GetRandomGamMode()}模式...";
    }
}
