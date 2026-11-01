using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Ipc;

internal sealed class NavmeshPathfindIPC
{
    private const string PathfindFailed = "[TTA] Navmesh Nav.Pathfind failed";

    private static NavmeshPathfindIPC? instance;

    private readonly ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>> pathfind;

    public static NavmeshPathfindIPC Instance => instance ?? (instance = new NavmeshPathfindIPC());

    private NavmeshPathfindIPC()
    {
        pathfind = Svc.PluginInterface.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>("vnavmesh.Nav.Pathfind");
    }

    public Task<List<Vector3>>? Pathfind(Vector3 from, Vector3 to, bool fly)
    {
        return IpcGate.Invoke<Task<List<Vector3>>?>(pathfind.HasFunction, () => pathfind.InvokeFunc(from, to, fly), null, PathfindFailed);
    }
}
