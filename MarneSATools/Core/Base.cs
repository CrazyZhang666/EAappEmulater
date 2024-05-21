using MarneSATools.Models;

namespace MarneSATools.Core;

public static class Base
{
    public static List<MapInfo> GameMapInfoDb { get; private set; } = new();

    public static List<ModeInfo> GameModeInfoDb { get; private set; } = new();

    static Base()
    {
        // 1 - 亚眠
        GameMapInfoDb.Add(new()
        {
            Name = "亚眠",
            DLC = "本体",
            Url = "Levels/MP/MP_Amiens/MP_Amiens",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 2 - 流血宴厅
        GameMapInfoDb.Add(new()
        {
            Name = "流血宴厅",
            DLC = "本体",
            Url = "Levels/MP/MP_Chateau/MP_Chateau",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 3 - 西奈沙漠
        GameMapInfoDb.Add(new()
        {
            Name = "西奈沙漠",
            DLC = "本体",
            Url = "Levels/MP/MP_Desert/MP_Desert",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 4 - 法欧堡
        GameMapInfoDb.Add(new()
        {
            Name = "法欧堡",
            DLC = "本体",
            Url = "Levels/MP/MP_FaoFortress/MP_FaoFortress",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 5 - 阿尔贡森林
        GameMapInfoDb.Add(new()
        {
            Name = "阿尔贡森林",
            DLC = "本体",
            Url = "Levels/MP/MP_Forest/MP_Forest",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 6 - 帝国边境（无前线模式）
        GameMapInfoDb.Add(new()
        {
            Name = "帝国边境",
            DLC = "本体",
            Url = "Levels/MP/MP_ItalianCoast/MP_ItalianCoast",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        });

        // 7 - 格拉巴山
        GameMapInfoDb.Add(new()
        {
            Name = "格拉巴山",
            DLC = "本体",
            Url = "Levels/MP/MP_MountainFort/MP_MountainFort",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 8 - 圣康坦的伤痕
        GameMapInfoDb.Add(new()
        {
            Name = "圣康坦的伤痕",
            DLC = "本体",
            Url = "Levels/MP/MP_Scar/MP_Scar",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 9 - 苏伊士
        GameMapInfoDb.Add(new()
        {
            Name = "苏伊士",
            DLC = "本体",
            Url = "Levels/MP/MP_Suez/MP_Suez",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        ///////////////////////////////

        // 10 - 庞然暗影
        GameMapInfoDb.Add(new()
        {
            Name = "庞然暗影",
            DLC = "DLC0",
            Url = "Xpack0/Levels/MP/MP_Giant/MP_Giant",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        ///////////////////////////////

        // 11 - 苏瓦松
        GameMapInfoDb.Add(new()
        {
            Name = "苏瓦松",
            DLC = "DLC1",
            Url = "Xpack1/Levels/MP_Fields/MP_Fields",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 12 - 决裂
        GameMapInfoDb.Add(new()
        {
            Name = "决裂",
            DLC = "DLC1",
            Url = "Xpack1/Levels/MP_Graveyard/MP_Graveyard",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 13 - 法乌克斯要塞
        GameMapInfoDb.Add(new()
        {
            Name = "法乌克斯要塞",
            DLC = "DLC1",
            Url = "Xpack1/Levels/MP_Underworld/MP_Underworld",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 14 - 凡尔登高地
        GameMapInfoDb.Add(new()
        {
            Name = "凡尔登高地",
            DLC = "DLC1",
            Url = "Xpack1/Levels/MP_Verdun/MP_Verdun",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        ///////////////////////////////

        // 15 - 攻占托尔
        GameMapInfoDb.Add(new()
        {
            Name = "攻占托尔",
            DLC = "DLC1-3",
            Url = "Xpack1-3/Levels/MP_ShovelTown/MP_ShovelTown",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        // 16 - 尼维尔之夜
        GameMapInfoDb.Add(new()
        {
            Name = "尼维尔之夜",
            DLC = "DLC1-3",
            Url = "Xpack1-3/Levels/MP_Trench/MP_Trench",
            Modes = new() { "Conquest0", "Rush0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        });

        ///////////////////////////////

        // 17 - 勃鲁希洛夫关口
        GameMapInfoDb.Add(new()
        {
            Name = "勃鲁希洛夫关口",
            DLC = "DLC2",
            Url = "Xpack2/Levels/MP/MP_Bridge/MP_Bridge",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        });

        // 18 - 阿尔比恩
        GameMapInfoDb.Add(new()
        {
            Name = "阿尔比恩",
            DLC = "DLC2",
            Url = "Xpack2/Levels/MP/MP_Islands/MP_Islands",
            Modes = new() { "Conquest0", "Rush0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        });

        // 19 - 武普库夫山口
        GameMapInfoDb.Add(new()
        {
            Name = "武普库夫山口",
            DLC = "DLC2",
            Url = "Xpack2/Levels/MP/MP_Ravines/MP_Ravines",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        });

        // 20 - 察里津
        GameMapInfoDb.Add(new()
        {
            Name = "察里津",
            DLC = "DLC2",
            Url = "Xpack2/Levels/MP/MP_Tsaritsyn/MP_Tsaritsyn",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        });

        // 21 - 加利西亚
        GameMapInfoDb.Add(new()
        {
            Name = "加利西亚",
            DLC = "DLC2",
            Url = "Xpack2/Levels/MP/MP_Valley/MP_Valley",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        });

        // 22 - 窝瓦河
        GameMapInfoDb.Add(new()
        {
            Name = "窝瓦河",
            DLC = "DLC2",
            Url = "Xpack2/Levels/MP/MP_Volga/MP_Volga",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        });

        ///////////////////////////////

        // 23 - 海丽丝岬
        GameMapInfoDb.Add(new()
        {
            Name = "海丽丝岬",
            DLC = "DLC3",
            Url = "Xpack3/Levels/MP/MP_Beachhead/MP_Beachhead",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        });

        // 24 - 泽布吕赫
        GameMapInfoDb.Add(new()
        {
            Name = "泽布吕赫",
            DLC = "DLC3",
            Url = "Xpack3/Levels/MP/MP_Harbor/MP_Harbor",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        });

        // 25 - 黑尔戈兰湾
        GameMapInfoDb.Add(new()
        {
            Name = "黑尔戈兰湾",
            DLC = "DLC3",
            Url = "Xpack3/Levels/MP/MP_Naval/MP_Naval",
            Modes = new() { "Conquest0" }
        });

        // 26 - 阿奇巴巴
        GameMapInfoDb.Add(new()
        {
            Name = "阿奇巴巴",
            DLC = "DLC3",
            Url = "Xpack3/Levels/MP/MP_Ridge/MP_Ridge",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        });

        ///////////////////////////////

        // 27 - 剃刀边缘
        GameMapInfoDb.Add(new()
        {
            Name = "剃刀边缘",
            DLC = "DLC4",
            Url = "Xpack4/Levels/MP/MP_Alps/MP_Alps",
            Modes = new() { "AirAssault0" }
        });

        // 28 - 伦敦的呼唤：夜袭
        GameMapInfoDb.Add(new()
        {
            Name = "伦敦的呼唤：夜袭",
            DLC = "DLC4",
            Url = "Xpack4/Levels/MP/MP_Blitz/MP_Blitz",
            Modes = new() { "AirAssault0" }
        });

        // 29 - 帕斯尚尔
        GameMapInfoDb.Add(new()
        {
            Name = "帕斯尚尔",
            DLC = "DLC4",
            Url = "Xpack4/Levels/MP/MP_Hell/MP_Hell",
            Modes = new() { "Conquest0", "Rush0", "Possession0", "Domination0", "TeamDeathMatch0" }
        });

        // 30 - 伦敦的呼唤：灾祸
        GameMapInfoDb.Add(new()
        {
            Name = "伦敦的呼唤：灾祸",
            DLC = "DLC4",
            Url = "Xpack4/Levels/MP/MP_London/MP_London",
            Modes = new() { "AirAssault0" }
        });

        // 31 - 索姆河
        GameMapInfoDb.Add(new()
        {
            Name = "索姆河",
            DLC = "DLC4",
            Url = "Xpack4/Levels/MP/MP_Offensive/MP_Offensive",
            Modes = new() { "Conquest0", "Rush0", "BreakthroughLarge0", "Breakthrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        });

        // 32 - 卡波雷托
        GameMapInfoDb.Add(new()
        {
            Name = "卡波雷托",
            DLC = "DLC4",
            Url = "Xpack4/Levels/MP/MP_River/MP_River",
            Modes = new() { "Conquest0", "Rush0", "Possession0", "Domination0", "TeamDeathMatch0" }
        });

        /////////////////////////////////////////////////////

        GameModeInfoDb.Add(new() { Code = "Conquest0", Name = "征服" });
        GameModeInfoDb.Add(new() { Code = "Rush0", Name = "突袭" });
        GameModeInfoDb.Add(new() { Code = "BreakthroughLarge0", Name = "行动模式" });
        GameModeInfoDb.Add(new() { Code = "Breakthrough0", Name = "闪击行动" });
        GameModeInfoDb.Add(new() { Code = "Possession0", Name = "战争信鸽" });
        GameModeInfoDb.Add(new() { Code = "TugOfWar0", Name = "前线" });
        GameModeInfoDb.Add(new() { Code = "Domination0", Name = "抢攻" });
        GameModeInfoDb.Add(new() { Code = "TeamDeathMatch0", Name = "团队死斗" });
        GameModeInfoDb.Add(new() { Code = "ZoneControl0", Name = "空降补给" });
        GameModeInfoDb.Add(new() { Code = "AirAssault0", Name = "空中突袭" });
    }
}
