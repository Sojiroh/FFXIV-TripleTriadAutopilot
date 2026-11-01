namespace AutoTripleTriadGrind.Core.Localization;

internal readonly struct LocPlural(string keyBase, string oneSource, string otherSource)
{
    public readonly string KeyBase = keyBase;

    public readonly string OneSource = oneSource;

    public readonly string OtherSource = otherSource;
}
