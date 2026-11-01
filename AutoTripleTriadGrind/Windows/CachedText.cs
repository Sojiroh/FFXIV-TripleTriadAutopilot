using System;
using AutoTripleTriadGrind.Core.Localization;

namespace AutoTripleTriadGrind.Windows;

internal struct CachedText
{
    private LanguageInfo? language;

    private long key;

    private string? text;

    public readonly bool TryGet(long key, out string text)
    {
        if (this.text != null && this.key == key && language == Loc.Current)
        {
            text = this.text;
            return true;
        }
        text = string.Empty;
        return false;
    }

    public string Set(long key, string text)
    {
        this.key = key;
        language = Loc.Current;
        this.text = text;
        return text;
    }

    public string Get(long key, Func<long, string> build)
    {
        if (!TryGet(key, out var cached))
        {
            return Set(key, build(key));
        }
        return cached;
    }
}
