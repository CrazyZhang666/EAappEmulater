using System.Collections.Generic;
using System.Linq;

namespace EAappEmulater.Helper
{
    public class LanguageEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
    }

    public static class LanguageConfigHelper
    {
        // Hard-coded list of supported languages. Keep in sync with Assets/Files/Lang/*.xaml
        private static readonly List<LanguageEntry> Languages = new()
        {
            new LanguageEntry { Code = "zh-CN", Name = "简体中文", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/zh_CN.png" },
            new LanguageEntry { Code = "en-US", Name = "English", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/en_US.png" },
            // Use Unicode escape for 'ç' to avoid any encoding issues
            new LanguageEntry { Code = "fr-FR", Name = "Fran\u00E7ais", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/fr_FR.png" },
            new LanguageEntry { Code = "de-DE", Name = "Deutsch", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/de_DE.png" },
            new LanguageEntry { Code = "it-IT", Name = "Italiano", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/it_IT.png" },
            new LanguageEntry { Code = "ja-JP", Name = "日本語", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/ja_JP.png" },
            new LanguageEntry { Code = "pt-BR", Name = "Português (BR)", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/pt_BR.png" },
            new LanguageEntry { Code = "ru-RU", Name = "Русский", Image = "pack://application:,,,/EAappEmulater;component/Assets/Images/Regions/ru_RU.png" }
        };

        public static List<LanguageEntry> GetLanguages()
        {
            // return a shallow copy to prevent external modification
            return Languages.Select(l => new LanguageEntry { Code = l.Code, Name = l.Name, Image = l.Image }).ToList();
        }

        public static LanguageEntry? FindByCode(string code)
        {
            return Languages.FirstOrDefault(x => x.Code == code);
        }
    }
}
