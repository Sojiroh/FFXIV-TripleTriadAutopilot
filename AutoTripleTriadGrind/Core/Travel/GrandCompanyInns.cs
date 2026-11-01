using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoTripleTriadGrind.Core.Travel;

internal static class GrandCompanyInns
{
    private static readonly CityInn limsaLominsa = new(128u, 177u, 1000974u, new Vector3(15.42688f, 39.99999f, 12.466553f));

    private static readonly CityInn gridania = new(132u, 179u, 1000102u, new Vector3(25.6627f, -8f, 99.74237f));

    private static readonly CityInn uldah = new(130u, 178u, 1001976u, new Vector3(28.85994f, 6.999999f, -80.12716f));

    public static CityInn For(byte grandCompany)
    {
        return grandCompany switch
        {
            2 => gridania,
            3 => uldah,
            _ => limsaLominsa,
        };
    }

    public unsafe static CityInn ForPlayer()
    {
        var playerState = PlayerState.Instance();
        return For((playerState == null) ? (byte)0 : playerState->GrandCompany);
    }
}
