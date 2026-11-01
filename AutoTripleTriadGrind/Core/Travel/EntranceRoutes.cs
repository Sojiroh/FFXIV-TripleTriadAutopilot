using System.Numerics;

namespace AutoTripleTriadGrind.Core.Travel;

internal static class EntranceRoutes
{
    private const uint ThePillarsTerritoryId = 419u;

    private const uint FortempsManorTerritoryId = 433u;

    private const uint DomanEnclaveTerritoryId = 759u;

    private const uint GangosTerritoryId = 915u;

    private const uint TheLochsTerritoryId = 621u;

    private const uint TerncliffTerritoryId = 919u;

    private const uint YakTelTerritoryId = 1189u;

    private const uint LowerJeunoTerritoryId = 1265u;

    private const uint HouseFortempsGuardId = 1011217u;

    private const uint GangosAirshipPilotId = 1032884u;

    private const uint TerncliffResistanceEscortId = 1033637u;

    private const uint LowerJeunoPassageId = 2014450u;

    private static readonly EntranceRoute[] routes = new EntranceRoute[]
    {
        new EntranceRoute(ThePillarsTerritoryId, FortempsManorTerritoryId, new Vector3(17.990356f, 16.009666f, -9.567444f), HouseFortempsGuardId),
        new EntranceRoute(DomanEnclaveTerritoryId, GangosTerritoryId, new Vector3(127.27539f, -4.1787133f, 98.68005f), GangosAirshipPilotId),
        new EntranceRoute(TheLochsTerritoryId, TerncliffTerritoryId, new Vector3(-533.422f, 8.1885f, -46.3176f), TerncliffResistanceEscortId),
        new EntranceRoute(YakTelTerritoryId, LowerJeunoTerritoryId, new Vector3(-528.37415f, -152.20874f, 671.13745f), LowerJeunoPassageId)
    };

    public static bool TryGet(uint targetTerritoryId, out EntranceRoute route)
    {
        for (var i = 0; i < routes.Length; i++)
        {
            if (routes[i].TargetTerritoryId == targetTerritoryId)
            {
                route = routes[i];
                return true;
            }
        }
        route = default;
        return false;
    }
}
