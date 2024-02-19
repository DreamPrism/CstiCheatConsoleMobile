using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace CstiCheatConsoleMobile
{
    public static class Localization
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LocalizationManager), "LoadLanguage")]
        public static void LoadLanguagePostfix()
        {
            var instance = LocalizationManager.Instance;
            if (instance == null || instance.Languages == null ||
                LocalizationManager.CurrentLanguage >= instance.Languages.Length) return;
            var langSetting = instance.Languages[LocalizationManager.CurrentLanguage];
            using (var stream = Assembly.GetExecutingAssembly()
                       .GetManifestResourceStream($"CstiCheatConsoleMobile.locale.{langSetting.LanguageName}.csv"))
            {
                if (stream ==null ||!stream.CanRead) return;
                using (var reader = new StreamReader(stream))
                {
                    var localizationString = reader.ReadToEnd();
                    var dictionary = CSVParser.LoadFromString(localizationString);
                    var regex = new Regex(@"\\n");
                    var currentTexts = LocalizationManager.CurrentTexts;
                    foreach (var item in dictionary)
                    {
                        if (!currentTexts.ContainsKey(item.Key) && item.Value.Count >= 2)
                            currentTexts.Add(item.Key, regex.Replace(item.Value.get_Item(1), "\n"));
                    }
                }
            }
        }
    }
}