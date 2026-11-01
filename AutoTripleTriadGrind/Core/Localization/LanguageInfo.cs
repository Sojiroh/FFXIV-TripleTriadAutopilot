namespace AutoTripleTriadGrind.Core.Localization;

internal sealed class LanguageInfo
{
    public string Code { get; }

    public string NativeName { get; }

    public string EnglishName { get; }

    public string CultureName { get; }

    public PluralKind PluralKind { get; }

    public ushort[]? ExtraGlyphRanges { get; }

    public LanguageInfo(string code, string nativeName, string englishName, string cultureName, PluralKind pluralKind, ushort[]? extraGlyphRanges)
    {
        Code = code;
        NativeName = nativeName;
        EnglishName = englishName;
        CultureName = cultureName;
        PluralKind = pluralKind;
        ExtraGlyphRanges = extraGlyphRanges;
    }
}
