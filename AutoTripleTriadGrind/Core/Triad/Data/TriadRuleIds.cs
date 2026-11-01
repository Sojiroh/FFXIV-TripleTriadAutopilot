namespace AutoTripleTriadGrind.Core.Triad.Data;

public static class TriadRuleIds
{
    public const int Count = 16;

    public static bool Has(ushort ruleMask, TriadRuleId rule)
    {
        return (ruleMask & (1 << (int)rule)) != 0;
    }

    public static ushort With(ushort ruleMask, TriadRuleId rule)
    {
        return (ushort)(ruleMask | (1 << (int)rule));
    }
}
