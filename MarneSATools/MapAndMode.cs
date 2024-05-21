namespace MarneSATools;

public static class MapAndMode
{
    public static Dictionary<string, MapInfo> GameMapModeDb { get; private set; } = new();

    static MapAndMode()
    {
        // 1 - 亚眠
        GameMapModeDb["MP_Amiens"] = new()
        {
            ChsName = "亚眠",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_Amiens/MP_Amiens",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 2 - 流血宴厅
        GameMapModeDb["MP_Chateau"] = new()
        {
            ChsName = "流血宴厅",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_Chateau/MP_Chateau",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 3 - 西奈沙漠
        GameMapModeDb["MP_Desert"] = new()
        {
            ChsName = "西奈沙漠",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_Desert/MP_Desert",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 4 - 法欧堡
        GameMapModeDb["MP_FaoFortress"] = new()
        {
            ChsName = "法欧堡",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_FaoFortress/MP_FaoFortress",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 5 - 阿尔贡森林
        GameMapModeDb["MP_Forest"] = new()
        {
            ChsName = "阿尔贡森林",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_Forest/MP_Forest",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 6 - 帝国边境（无前线模式）
        GameMapModeDb["MP_ItalianCoast"] = new()
        {
            ChsName = "帝国边境",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_ItalianCoast/MP_ItalianCoast",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        };

        // 7 - 格拉巴山
        GameMapModeDb["MP_MountainFort"] = new()
        {
            ChsName = "格拉巴山",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_MountainFort/MP_MountainFort",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 8 - 圣康坦的伤痕
        GameMapModeDb["MP_Scar"] = new()
        {
            ChsName = "圣康坦的伤痕",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_Scar/MP_Scar",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 9 - 苏伊士
        GameMapModeDb["MP_Suez"] = new()
        {
            ChsName = "苏伊士",
            DlcName = "本体",
            MapUrl = "Levels/MP/MP_Suez/MP_Suez",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        ///////////////////////////////

        // 10 - 庞然暗影
        GameMapModeDb["MP_Giant"] = new()
        {
            ChsName = "庞然暗影",
            DlcName = "DLC0",
            MapUrl = "Xpack0/Levels/MP/MP_Giant/MP_Giant",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        ///////////////////////////////

        // 11 - 苏瓦松
        GameMapModeDb["MP_Fields"] = new()
        {
            ChsName = "苏瓦松",
            DlcName = "DLC1",
            MapUrl = "Xpack1/Levels/MP_Fields/MP_Fields",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 12 - 决裂
        GameMapModeDb["MP_Graveyard"] = new()
        {
            ChsName = "决裂",
            DlcName = "DLC1",
            MapUrl = "Xpack1/Levels/MP_Graveyard/MP_Graveyard",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 13 - 法乌克斯要塞
        GameMapModeDb["MP_Underworld"] = new()
        {
            ChsName = "法乌克斯要塞",
            DlcName = "DLC1",
            MapUrl = "Xpack1/Levels/MP_Underworld/MP_Underworld",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 14 - 凡尔登高地
        GameMapModeDb["MP_Verdun"] = new()
        {
            ChsName = "凡尔登高地",
            DlcName = "DLC1",
            MapUrl = "Xpack1/Levels/MP_Verdun/MP_Verdun",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        ///////////////////////////////

        // 15 - 攻占托尔
        GameMapModeDb["MP_ShovelTown"] = new()
        {
            ChsName = "攻占托尔",
            DlcName = "DLC1-3",
            MapUrl = "Xpack1-3/Levels/MP_ShovelTown/MP_ShovelTown",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        // 16 - 尼维尔之夜
        GameMapModeDb["MP_Trench"] = new()
        {
            ChsName = "尼维尔之夜",
            DlcName = "DLC1-3",
            MapUrl = "Xpack1-3/Levels/MP_Trench/MP_Trench",
            GameModes = new() { "Conquest0", "Rush0", "Possession0", "TugOfWar0", "Domination0", "TeamDeathMatch0" }
        };

        ///////////////////////////////

        // 17 - 勃鲁希洛夫关口
        GameMapModeDb["MP_Bridge"] = new()
        {
            ChsName = "勃鲁希洛夫关口",
            DlcName = "DLC2",
            MapUrl = "Xpack2/Levels/MP/MP_Bridge/MP_Bridge",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        };

        // 18 - 阿尔比恩
        GameMapModeDb["MP_Islands"] = new()
        {
            ChsName = "阿尔比恩",
            DlcName = "DLC2",
            MapUrl = "Xpack2/Levels/MP/MP_Islands/MP_Islands",
            GameModes = new() { "Conquest0", "Rush0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        };

        // 19 - 武普库夫山口
        GameMapModeDb["MP_Ravines"] = new()
        {
            ChsName = "武普库夫山口",
            DlcName = "DLC2",
            MapUrl = "Xpack2/Levels/MP/MP_Ravines/MP_Ravines",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        };

        // 20 - 察里津
        GameMapModeDb["MP_Tsaritsyn"] = new()
        {
            ChsName = "察里津",
            DlcName = "DLC2",
            MapUrl = "Xpack2/Levels/MP/MP_Tsaritsyn/MP_Tsaritsyn",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        };

        // 21 - 加利西亚
        GameMapModeDb["MP_Valley"] = new()
        {
            ChsName = "加利西亚",
            DlcName = "DLC2",
            MapUrl = "Xpack2/Levels/MP/MP_Valley/MP_Valley",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        };

        // 22 - 窝瓦河
        GameMapModeDb["MP_Volga"] = new()
        {
            ChsName = "窝瓦河",
            DlcName = "DLC2",
            MapUrl = "Xpack2/Levels/MP/MP_Volga/MP_Volga",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0", "ZoneControl0" }
        };

        ///////////////////////////////

        // 23 - 海丽丝岬
        GameMapModeDb["MP_Beachhead"] = new()
        {
            ChsName = "海丽丝岬",
            DlcName = "DLC3",
            MapUrl = "Xpack3/Levels/MP/MP_Beachhead/MP_Beachhead",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        };

        // 24 - 泽布吕赫
        GameMapModeDb["MP_Harbor"] = new()
        {
            ChsName = "泽布吕赫",
            DlcName = "DLC3",
            MapUrl = "Xpack3/Levels/MP/MP_Harbor/MP_Harbor",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        };

        // 25 - 黑尔戈兰湾
        GameMapModeDb["MP_Naval"] = new()
        {
            ChsName = "黑尔戈兰湾",
            DlcName = "DLC3",
            MapUrl = "Xpack3/Levels/MP/MP_Naval/MP_Naval",
            GameModes = new() { "Conquest0" }
        };

        // 26 - 阿奇巴巴
        GameMapModeDb["MP_Ridge"] = new()
        {
            ChsName = "阿奇巴巴",
            DlcName = "DLC3",
            MapUrl = "Xpack3/Levels/MP/MP_Ridge/MP_Ridge",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        };

        ///////////////////////////////

        // 27 - 剃刀边缘
        GameMapModeDb["MP_Alps"] = new()
        {
            ChsName = "剃刀边缘",
            DlcName = "DLC4",
            MapUrl = "Xpack4/Levels/MP/MP_Alps/MP_Alps",
            GameModes = new() { "AirAssault0" }
        };

        // 28 - 伦敦的呼唤：夜袭
        GameMapModeDb["MP_Blitz"] = new()
        {
            ChsName = "伦敦的呼唤：夜袭",
            DlcName = "DLC4",
            MapUrl = "Xpack4/Levels/MP/MP_Blitz/MP_Blitz",
            GameModes = new() { "AirAssault0" }
        };

        // 29 - 帕斯尚尔
        GameMapModeDb["MP_Hell"] = new()
        {
            ChsName = "帕斯尚尔",
            DlcName = "DLC4",
            MapUrl = "Xpack4/Levels/MP/MP_Hell/MP_Hell",
            GameModes = new() { "Conquest0", "Rush0", "Possession0", "Domination0", "TeamDeathMatch0" }
        };

        // 30 - 伦敦的呼唤：灾祸
        GameMapModeDb["MP_London"] = new()
        {
            ChsName = "伦敦的呼唤：灾祸",
            DlcName = "DLC4",
            MapUrl = "Xpack4/Levels/MP/MP_London/MP_London",
            GameModes = new() { "AirAssault0" }
        };

        // 31 - 索姆河
        GameMapModeDb["MP_Offensive"] = new()
        {
            ChsName = "索姆河",
            DlcName = "DLC4",
            MapUrl = "Xpack4/Levels/MP/MP_Offensive/MP_Offensive",
            GameModes = new() { "Conquest0", "Rush0", "BreakThroughLarge0", "BreakThrough0", "Possession0", "Domination0", "TeamDeathMatch0" }
        };

        // 32 - 卡波雷托
        GameMapModeDb["MP_River"] = new()
        {
            ChsName = "卡波雷托",
            DlcName = "DLC4",
            MapUrl = "Xpack4/Levels/MP/MP_River/MP_River",
            GameModes = new() { "Conquest0", "Rush0", "Possession0", "Domination0", "TeamDeathMatch0" }
        };
    }
}
