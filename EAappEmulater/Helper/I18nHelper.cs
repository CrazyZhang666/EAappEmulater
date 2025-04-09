using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAappEmulater.Helper;
internal class I18nHelper
{
    public static class I18n
    {
        public static string _(string key, params object[] args)
        {
            var raw = Application.Current.Resources[key]?.ToString() ?? $"[{key}]";
            return args.Length > 0 ? string.Format(raw, args) : raw;
        }
    }

    
}
