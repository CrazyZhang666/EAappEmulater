﻿using EAappEmulater.Enums;
using EAappEmulater.Models;

namespace EAappEmulater.Core;

public static class Base
{
    public static Dictionary<GameType, GameInfo> GameInfoDb { get; private set; } = new();
    public static Dictionary<string, List<string>> GameRegistryDb { get; private set; } = new();

    public static Dictionary<string, LocaleInfo> GameLocaleDb { get; private set; } = new();

    static Base()
    {
        GameInfoDb[GameType.BF3] = new()
        {
            GameType = GameType.BF3,
            Name = "战地风云3",
            Name2 = "Battlefield 3",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/BF3.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "bf3.exe",
            ContentId = "71067",
            Regedit = "SOFTWARE\\EA Games\\Battlefield 3",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 3",
            Locales = new() { "zh_TW", "fr_FR", "ko_KR", "it_IT", "cs_CZ", "ja_JP", "de_DE", "es_ES", "pl_PL", "en_US" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.BF4] = new()
        {
            GameType = GameType.BF4,
            Name = "战地风云4",
            Name2 = "Battlefield 4",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/BF4.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "bf4.exe",
            ContentId = "1015362",
            Regedit = "SOFTWARE\\EA Games\\Battlefield 4",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 4",
            Locales = new() { "en_US", "fr_FR", "it_IT", "de_DE", "es_ES", "pl_PL", "ru_RU", "pt_BR", "ja_JP", "cs_CZ", "ko_KR", "zh_TW" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.BFH] = new()
        {
            GameType = GameType.BFH,
            Name = "战地风云 硬仗",
            Name2 = "Battlefield : Hardline",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/BFH.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "bfh.exe",
            ContentId = "1013920",
            Regedit = "SOFTWARE\\EA Games\\BFH",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\BFH",
            Locales = new() { "en_US", "fr_FR", "it_IT", "de_DE", "es_ES", "pl_PL", "ru_RU", "pt_BR", "ja_JP", "cs_CZ", "ko_KR", "zh_TW" },
            IsOldLSX = true
        };

        GameInfoDb[GameType.BF1] = new()
        {
            GameType = GameType.BF1,
            Name = "战地风云1",
            Name2 = "Battlefield 1",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/BF1.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "bf1.exe",
            ContentId = "1026023",
            Regedit = "SOFTWARE\\EA Games\\Battlefield 1",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 1",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "pt_BR", "es_MX", "ar_SA", "tr_TR" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.BFV] = new()
        {
            GameType = GameType.BFV,
            Name = "战地风云V",
            Name2 = "Battlefield V",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/BFV.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = true,
            AppName = "EAAntiCheat.GameServiceLauncher.exe",
            ContentId = "196216",
            Regedit = "SOFTWARE\\EA Games\\Battlefield V",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield V",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "zh_CN", "pt_BR", "es_MX", "ar_SA", "ko_KR" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.BF2042] = new()
        {
            GameType = GameType.BF2042,
            Name = "战地风云2042",
            Name2 = "Battlefield 2042",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/BF2042.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = true,
            AppName = "EAAntiCheat.GameServiceLauncher.exe",
            ContentId = "193874",
            Regedit = "SOFTWARE\\EA Games\\Battlefield 2042",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Battlefield 2042",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "es_MX", "it_IT", "ja_JP", "ru_RU", "pl_PL", "zh_TW", "zh_CN", "ko_KR", "pt_BR", "ar_SA" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.F123] = new()
        {
            GameType = GameType.F123,
            Name = "F1® 23",
            Name2 = "EA SPORTS™ F1® 23 3",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/F123.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "F1_23.exe",
            ContentId = "16425635",
            Regedit = "SOFTWARE\\Codemasters\\F1_23",
            Regedit2 = "SOFTWARE\\WOW6432Node\\Codemasters\\F1_23",
            Locales = new() { "en_GB", "en_US", "fr_FR", "de_DE", "it_IT", "es_ES", "ja_JP", "nl_NL", "pl_PL", "pt_BR", "zh_CN", "ar_SA", "es_MX" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.FC24] = new()
        {
            GameType = GameType.FC24,
            Name = "FC™ 24",
            Name2 = "EA SPORTS FC™ 24",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/FC24.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = true,
            AppName = "EAAntiCheat.GameServiceLauncher.exe",
            ContentId = "198235",
            Regedit = "SOFTWARE\\EA Sports\\EA SPORTS FC 24",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Sports\\EA SPORTS FC 24",
            Locales = new() { "pt_PT", "tr_TR", "ko_KR", "cs_CZ", "zh_CN", "zh_HK", "da_DK", "no_NO", "sv_SE", "en_US", "pt_BR", "de_DE", "es_ES", "fr_FR", "it_IT", "ja_JP", "es_MX", "nl_NL", "pl_PL", "ru_RU", "ar_SA" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.FIFA22] = new()
        {
            GameType = GameType.FIFA22,
            Name = "FIFA 22",
            Name2 = "EA SPORTS™ FIFA 22",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/FIFA22.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "FIFA22.exe",
            ContentId = "196837",
            Regedit = "SOFTWARE\\EA Sports\\FIFA 22",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Sports\\FIFA 22",
            Locales = new() { "pt_PT", "tr_TR", "ko_KR", "cs_CZ", "zh_CN", "zh_HK", "da_DK", "no_NO", "sv_SE", "en_US", "pt_BR", "de_DE", "es_ES", "fr_FR", "it_IT", "ja_JP", "es_MX", "nl_NL", "pl_PL", "ru_RU", "ar_SA" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.FIFA23] = new()
        {
            GameType = GameType.FIFA23,
            Name = "FIFA 23",
            Name2 = "EA SPORTS™ FIFA 23",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/FIFA23.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = true,
            AppName = "EAAntiCheat.GameServiceLauncher.exe",
            ContentId = "16115019",
            Regedit = "SOFTWARE\\EA Sports\\FIFA 23",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Sports\\FIFA 23",
            Locales = new() { "pt_PT", "tr_TR", "ko_KR", "cs_CZ", "zh_CN", "zh_HK", "da_DK", "no_NO", "sv_SE", "en_US", "pt_BR", "de_DE", "es_ES", "fr_FR", "it_IT", "ja_JP", "es_MX", "nl_NL", "pl_PL", "ru_RU", "ar_SA" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.ITT] = new()
        {
            GameType = GameType.ITT,
            Name = "双人成行",
            Name2 = "It Takes Two",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/ITT.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "ItTakesTwo.exe",
            ContentId = "16050355",
            Regedit = "SOFTWARE\\Hazelight\\ItTakesTwo",
            Regedit2 = "SOFTWARE\\WOW6432Node\\Hazelight\\ItTakesTwo",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "pt_BR", "es_MX", "ar_SA", "zh_CN" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.NFS19] = new()
        {
            GameType = GameType.NFS19,
            Name = "极品飞车19",
            Name2 = "Need for Speed™",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/NFS19.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "NFS16.exe",
            ContentId = "1024486",
            Regedit = "SOFTWARE\\EA Games\\Need for Speed",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Need for Speed",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "pt_BR", "es_MX", "ar_SA", "zh_CN" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.NFS21] = new()
        {
            GameType = GameType.NFS21,
            Name = "极品飞车 : 热度",
            Name2 = "Need for Speed™ Heat",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/NFS21.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "NeedForSpeedHeat.exe",
            ContentId = "195133",
            Regedit = "SOFTWARE\\EA Games\\Need for Speed Heat",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Need for Speed Heat",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "pt_BR", "es_MX", "ar_SA", "zh_CN" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.NFS22] = new()
        {
            GameType = GameType.NFS22,
            Name = "极品飞车 : 不羁",
            Name2 = "Need For Speed™ : Unbound",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/NFS22.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "NeedForSpeedUnbound.exe",
            ContentId = "196787",
            Regedit = "SOFTWARE\\EA Games\\Need for Speed Unbound",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\Need for Speed Unbound",
            Locales = new() { "en_US", "fr_FR", "es_ES", "de_DE", "it_IT", "pl_PL", "pt_BR", "ja_JP", "zh_TW", "zh_CN", "ar_SA", "ko_KR" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.PVZGN] = new()
        {
            GameType = GameType.PVZGN,
            Name = "植物大战僵尸 : 和睦小镇保卫战",
            Name2 = "Plants vs Zombies Battle for Neighborville",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/PVZBN.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = true,
            AppName = "EAAntiCheat.GameServiceLauncher.exe",
            ContentId = "194814",
            Regedit = "SOFTWARE\\PopCap\\PVZ Battle for Neighborville",
            Regedit2 = "SOFTWARE\\WOW6432Node\\PopCap\\PVZ Battle for Neighborville",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "pt_BR", "es_MX", "ar_SA", "zh_CN" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.PVZGW] = new()
        {
            GameType = GameType.PVZGW,
            Name = "植物大战僵尸 : 花园战争",
            Name2 = "PVZ Garden Warfare",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/PVZGW.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "PVZ.Main_Win64_Retail.exe",
            ContentId = "1014748",
            Regedit = "SOFTWARE\\PopCap\\Plants vs Zombies Garden Warfare",
            Regedit2 = "SOFTWARE\\WOW6432Node\\PopCap\\Plants vs Zombies Garden Warfare",
            Locales = new() { "en_US", "fr_FR", "es_ES", "de_DE", "it_IT", "pt_BR" },
            IsOldLSX = true
        };

        GameInfoDb[GameType.PVZGW2] = new()
        {
            GameType = GameType.PVZGW2,
            Name = "植物大战僵尸 : 花园战争2",
            Name2 = "Plants vs. Zombies Garden Warfare 2",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/PVZGW2.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = true,
            AppName = "EAAntiCheat.GameServiceLauncher.exe",
            ContentId = "1026482",
            Regedit = "SOFTWARE\\PopCap\\Plants vs Zombies GW2",
            Regedit2 = "SOFTWARE\\WOW6432Node\\PopCap\\Plants vs Zombies GW2",
            Locales = new() { "en_US", "fr_FR", "it_IT", "de_DE", "es_ES", "pt_BR", "pl_PL", "zh_TW" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.SWBF1] = new()
        {
            GameType = GameType.SWBF1,
            Name = "星球大战 : 战场前线",
            Name2 = "STAR WARS Battlefront",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/SWBF1.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "starwarsbattlefront.exe",
            ContentId = "1024390",
            Regedit = "SOFTWARE\\EA Games\\STAR WARS Battlefront",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\STAR WARS Battlefront",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "pt_BR", "es_MX", "ar_SA" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.SWBF2] = new()
        {
            GameType = GameType.SWBF2,
            Name = "星球大战 : 战场前线2",
            Name2 = "Star Wars : BattleFront II",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/SWBF2.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "starwarsbattlefrontii.exe",
            ContentId = "1035052",
            Regedit = "SOFTWARE\\EA Games\\STAR WARS Battlefront II",
            Regedit2 = "SOFTWARE\\WOW6432Node\\EA Games\\STAR WARS Battlefront II",
            Locales = new() { "en_US", "fr_FR", "de_DE", "es_ES", "it_IT", "ru_RU", "pl_PL", "ja_JP", "zh_TW", "pt_BR", "es_MX" },
            IsOldLSX = false
        };

        GameInfoDb[GameType.TTF2] = new()
        {
            GameType = GameType.TTF2,
            Name = "泰坦陨落2",
            Name2 = "Titanfall 2",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Games/TTF2.jpg",
            IsUseCustom = false,
            Dir = string.Empty,
            Args = string.Empty,
            Dir2 = string.Empty,
            Args2 = string.Empty,
            IsInstalled = false,
            IsEAAC = false,
            AppName = "Titanfall2.exe",
            ContentId = "1039093",
            Regedit = "SOFTWARE\\Respawn\\Titanfall2",
            Regedit2 = "SOFTWARE\\WOW6432Node\\Respawn\\Titanfall2",
            Locales = new() { "en_US", "fr_FR", "de_DE", "it_IT", "es_ES", "pl_PL", "ru_RU", "es_MX", "ja_JP", "pt_BR", "zh_TW" },
            IsOldLSX = false
        };

        ////////////////////////////////////////////////////

        // 提前缓存游戏注册表信息
        foreach (var item in GameInfoDb)
        {
            GameRegistryDb.Add(item.Value.ContentId, new()
            {
                item.Value.Regedit,
                item.Value.Regedit2
            });
        }

        ////////////////////////////////////////////////////

        GameLocaleDb["NULL"] = new()
        {
            Code = "NULL",
            Name = "未知语言",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/NULL.png"
        };

        GameLocaleDb["ar_SA"] = new()
        {
            Code = "ar_SA",
            Name = "阿拉伯语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/ar_SA.png"
        };

        GameLocaleDb["cs_CZ"] = new()
        {
            Code = "cs_CZ",
            Name = "捷克语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/cs_CZ.png"
        };

        GameLocaleDb["da_DK"] = new()
        {
            Code = "da_DK",
            Name = "丹麦语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/da_DK.png"
        };

        GameLocaleDb["de_DE"] = new()
        {
            Code = "de_DE",
            Name = "德语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/de_DE.png"
        };

        GameLocaleDb["en_GB"] = new()
        {
            Code = "en_GB",
            Name = "英语 - 英国",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/en_GB.png"
        };

        GameLocaleDb["en_US"] = new()
        {
            Code = "en_US",
            Name = "英语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/en_US.png"
        };

        GameLocaleDb["es_ES"] = new()
        {
            Code = "es_ES",
            Name = "西班牙语 - 西班牙",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/es_ES.png"
        };

        GameLocaleDb["es_MX"] = new()
        {
            Code = "es_MX",
            Name = "西班牙语 - 拉丁美洲",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/es_MX.png"
        };

        GameLocaleDb["fr_FR"] = new()
        {
            Code = "fr_FR",
            Name = "法语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/fr_FR.png"
        };

        GameLocaleDb["it_IT"] = new()
        {
            Code = "it_IT",
            Name = "意大利语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/it_IT.png"
        };

        GameLocaleDb["ja_JP"] = new()
        {
            Code = "ja_JP",
            Name = "日语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/ja_JP.png"
        };

        GameLocaleDb["ko_KR"] = new()
        {
            Code = "ko_KR",
            Name = "韩语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/ko_KR.png"
        };

        GameLocaleDb["nl_NL"] = new()
        {
            Code = "nl_NL",
            Name = "荷兰语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/nl_NL.png"
        };

        GameLocaleDb["no_NO"] = new()
        {
            Code = "no_NO",
            Name = "挪威语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/no_NO.png"
        };

        GameLocaleDb["pl_PL"] = new()
        {
            Code = "pl_PL",
            Name = "波兰语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/pl_PL.png"
        };

        GameLocaleDb["pt_BR"] = new()
        {
            Code = "pt_BR",
            Name = "葡萄牙语 - 巴西",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/pt_BR.png"
        };

        GameLocaleDb["pt_PT"] = new()
        {
            Code = "pt_PT",
            Name = "葡萄牙语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/pt_PT.png"
        };

        GameLocaleDb["ru_RU"] = new()
        {
            Code = "ru_RU",
            Name = "俄语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/ru_RU.png"
        };

        GameLocaleDb["sv_SE"] = new()
        {
            Code = "sv_SE",
            Name = "瑞典语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/sv_SE.png"
        };

        GameLocaleDb["tr_TR"] = new()
        {
            Code = "tr_TR",
            Name = "土耳其语",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/tr_TR.png"
        };

        GameLocaleDb["zh_CN"] = new()
        {
            Code = "zh_CN",
            Name = "简体中文",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/zh_CN.png"
        };

        GameLocaleDb["zh_HK"] = new()
        {
            Code = "zh_HK",
            Name = "繁体中文",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/zh_HK.png"
        };

        GameLocaleDb["zh_TW"] = new()
        {
            Code = "zh_TW",
            Name = "繁体中文",
            Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/zh_TW.png"
        };
    }
}
