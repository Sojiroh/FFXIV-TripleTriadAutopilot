using System;

namespace AutoTripleTriadGrind.Core.Localization;

internal static class Languages
{
    private static readonly ushort[] JapaneseGlyphRanges = new ushort[10] { 12288, 12351, 12352, 12447, 12448, 12543, 12784, 12799, 65280, 65519 };

    private static readonly ushort[] ChineseGlyphRanges = new ushort[4] { 12288, 12351, 65280, 65519 };

    private static readonly ushort[] CyrillicGlyphRanges = new ushort[2] { 1024, 1279 };

    public static readonly LanguageInfo English = new LanguageInfo("en", "English", "English", "en-US", PluralKind.EnglishLike, null);

    public static readonly LanguageInfo French = new LanguageInfo("fr", "Français", "French", "fr-FR", PluralKind.French, null);

    public static readonly LanguageInfo German = new LanguageInfo("de", "Deutsch", "German", "de-DE", PluralKind.EnglishLike, null);

    public static readonly LanguageInfo Turkish = new LanguageInfo("tr", "Türkçe", "Turkish", "tr-TR", PluralKind.EnglishLike, null);

    public static readonly LanguageInfo Spanish = new LanguageInfo("es", "Español", "Spanish", "es-ES", PluralKind.EnglishLike, null);

    public static readonly LanguageInfo Portuguese = new LanguageInfo("pt", "Português (Brasil)", "Portuguese", "pt-BR", PluralKind.EnglishLike, null);

    public static readonly LanguageInfo Russian = new LanguageInfo("ru", "Русский", "Russian", "ru-RU", PluralKind.EnglishLike, CyrillicGlyphRanges);

    public static readonly LanguageInfo Japanese = new LanguageInfo("ja", "日本語", "Japanese", "ja-JP", PluralKind.EnglishLike, JapaneseGlyphRanges);

    public static readonly LanguageInfo Chinese = new LanguageInfo("zh", "中文", "Chinese", "zh-CN", PluralKind.EnglishLike, ChineseGlyphRanges);

    public static readonly LanguageInfo[] All = new LanguageInfo[9] { English, French, German, Turkish, Spanish, Portuguese, Russian, Japanese, Chinese };

    public static LanguageInfo Resolve(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return English;
        }
        for (int i = 0; i < All.Length; i++)
        {
            if (string.Equals(All[i].Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return All[i];
            }
        }
        return English;
    }

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }
        for (int i = 0; i < All.Length; i++)
        {
            if (string.Equals(All[i].Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
