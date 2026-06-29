using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class SelectLanguageService
{
    public static List<Language> systemLanguages;

    static SelectLanguageService()
    {
        systemLanguages = new List<Language>
        {
            new("en", "English", SystemLanguage.English),
            new("vi", "Tiếng Việt", SystemLanguage.Vietnamese),
            // new Language("pt", "Português", SystemLanguage.Portuguese),
            // new Language("th", "ไทย", SystemLanguage.Thai),
            // new Language("zhcn", "简体中文", SystemLanguage.ChineseSimplified),
            // new Language("ko", "한국어", SystemLanguage.Korean),
            // new Language("ja", "日本語", SystemLanguage.Japanese),
            new("de", "Deutsch", SystemLanguage.German),
            // new Language("it", "Italia", SystemLanguage.Italian),
            new("es", "Español", SystemLanguage.Spanish),
            // new Language("id", "Indonesia", SystemLanguage.Indonesian),
            // new Language("ru", "Русский", SystemLanguage.Russian),
            // new Language("fr", "Français", SystemLanguage.French),
            // new Language("tr", "Türkiye", SystemLanguage.Turkish),
            // new Language("pl", "Język polski", SystemLanguage.Polish),
            new("nl", "Nederlands", SystemLanguage.Dutch)
            //new Language("zhtw", "繁體中文", SystemLanguage.ChineseTraditional),
        };
    }

    public static string GetPrefixLanguage(SystemLanguage systemLanguage)
    {
        var data = systemLanguages.Find(x => x.systemLanguage == systemLanguage);
        if (data != null) return data.prefix;

        return "en";
    }

    public static CultureInfo FromIsoName(string name)
    {
        return CultureInfo
            .GetCultures(CultureTypes.NeutralCultures)
            .FirstOrDefault(c => c.ThreeLetterISOLanguageName == name);
    }
}