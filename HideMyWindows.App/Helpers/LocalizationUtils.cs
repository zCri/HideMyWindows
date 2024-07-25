using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFLocalizeExtension.Engine;

namespace HideMyWindows.App.Helpers
{
    internal class LocalizationUtils
    {
        public static string GetString(string key, string? defaultValue = null)
        {
            return LocalizeDictionary.Instance.GetLocalizedObject("HideMyWindows.App", "Strings", key, LocalizeDictionary.CurrentCulture) as string ?? (defaultValue ?? $"Key: {key}");
        }
    }
}
