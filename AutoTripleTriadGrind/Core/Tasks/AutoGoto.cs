using System;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.External;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Travel;

namespace AutoTripleTriadGrind.Core.Tasks;

internal sealed class AutoGoto(uint territoryId, Vector3 destination) : AutoCommon
{
    private const float ArriveWithinMeters = 3f;

    private const string StopArgument = "stop";

    private const string Usage = "[TTA] Usage: /tta goto <territoryId> <x> <y> <z>, or /tta goto stop.";

    private static readonly char[] ArgumentSeparators = new char[] { ' ', ',' };

    public static void HandleCommand(string arguments, bool runActive)
    {
        var automation = clib.Services.Svc.Automation;
        if (arguments.Equals(StopArgument, StringComparison.OrdinalIgnoreCase))
        {
            if (!(automation.CurrentTask is AutoGoto))
            {
                ECommons.DalamudServices.Svc.Chat.Print("[TTA] No goto is running.");
                return;
            }
            automation.Stop();
            ECommons.DalamudServices.Svc.Chat.Print("[TTA] Goto stopped.");
            return;
        }
        if (!TryParse(arguments, out var parsedTerritory, out var parsedDestination))
        {
            ECommons.DalamudServices.Svc.Chat.PrintError(Usage);
            return;
        }
        if (automation.CurrentTask is AutoGoto)
        {
            ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] A goto is already running. /tta goto stop cancels it.");
            return;
        }
        if (runActive)
        {
            ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] Stop the run before using goto.");
            return;
        }
        if (!ExternalPlugins.IsInstalled(ExternalPlugin.Vnavmesh))
        {
            ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] Goto needs the pathfinding plugin listed on the Plugins page.");
            return;
        }
        ECommons.DalamudServices.Svc.Chat.Print($"{AttgConstants.LogPrefix} Goto: heading to {TerritoryNames.Of(parsedTerritory)} ({parsedTerritory}) at {parsedDestination.X:F1}, {parsedDestination.Y:F1}, {parsedDestination.Z:F1}.");
        automation.Start(new AutoGoto(parsedTerritory, parsedDestination));
    }

    protected override async Task Execute()
    {
        var zoneName = TerritoryNames.Of(territoryId);
        Diag($"Goto: travelling to {zoneName} ({territoryId}) at {destination}.");
        try
        {
            var arrived = await TravelTo(territoryId, destination, ArriveWithinMeters);
            if (CancelToken.IsCancellationRequested)
            {
                Diag("Goto: cancelled.");
                return;
            }
            if (!arrived)
            {
                ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] Goto: could not reach the spot in " + zoneName + ". The log has the details.");
                return;
            }
            Status = "Arrived";
            ECommons.DalamudServices.Svc.Chat.Print("[TTA] Goto: arrived in " + zoneName + ".");
        }
        finally
        {
            NavmeshIPC.Instance.Stop();
        }
    }

    private static bool TryParse(string arguments, out uint territoryId, out Vector3 destination)
    {
        territoryId = 0u;
        destination = default;
        var parts = arguments.Split(ArgumentSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            return false;
        }
        if (!uint.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out territoryId))
        {
            return false;
        }
        if (!TryParseCoordinate(parts[1], out var x) || !TryParseCoordinate(parts[2], out var y) || !TryParseCoordinate(parts[3], out var z))
        {
            return false;
        }
        destination = new Vector3(x, y, z);
        return true;
    }

    private static bool TryParseCoordinate(string text, out float value)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return float.IsFinite(value);
        }
        return false;
    }
}
