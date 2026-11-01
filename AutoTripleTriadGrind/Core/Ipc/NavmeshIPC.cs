using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Ipc;

internal sealed class NavmeshIPC
{
    public const int WaypointsUnavailable = -1;

    private const float AboveEveryTerrainY = 1024f;

    private const float NoTolerance = 0f;

    private const float BuildIdle = -1f;

    private const string IsReadyFailed = "[TTA] Navmesh IsReady failed";

    private const string BuildProgressFailed = "[TTA] Navmesh BuildProgress failed";

    private const string IsRunningFailed = "[TTA] Navmesh IsRunning failed";

    private const string SimpleMovePathfindFailed = "[TTA] Navmesh SimpleMove.PathfindInProgress failed";

    private const string NavPathfindFailed = "[TTA] Navmesh Nav.PathfindInProgress failed";

    private const string NearestPointFailed = "[TTA] Navmesh NearestPoint failed";

    private const string NearestPointReachableFailed = "[TTA] Navmesh NearestPointReachable failed";

    private const string PointOnFloorFailed = "[TTA] Navmesh PointOnFloor failed";

    private const string NumWaypointsFailed = "[TTA] Navmesh NumWaypoints failed";

    private const string ListWaypointsFailed = "[TTA] Navmesh ListWaypoints failed";

    private const string StopFailed = "[TTA] Navmesh Stop failed";

    private const string MoveToFailed = "[TTA] Navmesh Path.MoveTo failed";

    private const string GetToleranceFailed = "[TTA] Navmesh Path.GetTolerance failed";

    private const string PathfindAndMoveCloseToFailed = "[TTA] Navmesh SimpleMove.PathfindAndMoveCloseTo failed";

    private static NavmeshIPC? instance;

    private readonly ICallGateSubscriber<bool> pathIsRunning;

    private readonly ICallGateSubscriber<bool> simpleMovePathfindInProgress;

    private readonly ICallGateSubscriber<bool> navPathfindInProgress;

    private readonly ICallGateSubscriber<bool> navIsReady;

    private readonly ICallGateSubscriber<float> navBuildProgress;

    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPoint;

    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPointReachable;

    private readonly ICallGateSubscriber<Vector3, bool, float, Vector3?> pointOnFloor;

    private readonly ICallGateSubscriber<object> pathStop;

    private readonly ICallGateSubscriber<List<Vector3>, bool, object> pathMoveTo;

    private readonly ICallGateSubscriber<int> pathNumWaypoints;

    private readonly ICallGateSubscriber<List<Vector3>> pathListWaypoints;

    private readonly ICallGateSubscriber<float> pathGetTolerance;

    private readonly ICallGateSubscriber<Vector3, bool, float, bool> simpleMovePathfindAndMoveCloseTo;

    private readonly Func<bool> isRunningCall;

    private readonly Func<bool> simpleMovePathfindInProgressCall;

    private readonly Func<bool> navPathfindInProgressCall;

    private readonly Func<bool> isReadyCall;

    private readonly Func<float> buildProgressCall;

    private readonly Func<int> numWaypointsCall;

    private readonly Func<List<Vector3>?> listWaypointsCall;

    private readonly Func<float> getToleranceCall;

    private readonly Action stopCall;

    public static NavmeshIPC Instance => instance ?? (instance = new NavmeshIPC());

    private NavmeshIPC()
    {
        var pluginInterface = Svc.PluginInterface;
        pathIsRunning = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        simpleMovePathfindInProgress = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");
        navPathfindInProgress = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.PathfindInProgress");
        navIsReady = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        navBuildProgress = pluginInterface.GetIpcSubscriber<float>("vnavmesh.Nav.BuildProgress");
        nearestPoint = pluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPoint");
        nearestPointReachable = pluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPointReachable");
        pointOnFloor = pluginInterface.GetIpcSubscriber<Vector3, bool, float, Vector3?>("vnavmesh.Query.Mesh.PointOnFloor");
        pathStop = pluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
        pathMoveTo = pluginInterface.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo");
        pathNumWaypoints = pluginInterface.GetIpcSubscriber<int>("vnavmesh.Path.NumWaypoints");
        pathListWaypoints = pluginInterface.GetIpcSubscriber<List<Vector3>>("vnavmesh.Path.ListWaypoints");
        pathGetTolerance = pluginInterface.GetIpcSubscriber<float>("vnavmesh.Path.GetTolerance");
        simpleMovePathfindAndMoveCloseTo = pluginInterface.GetIpcSubscriber<Vector3, bool, float, bool>("vnavmesh.SimpleMove.PathfindAndMoveCloseTo");
        isRunningCall = pathIsRunning.InvokeFunc;
        simpleMovePathfindInProgressCall = simpleMovePathfindInProgress.InvokeFunc;
        navPathfindInProgressCall = navPathfindInProgress.InvokeFunc;
        isReadyCall = navIsReady.InvokeFunc;
        buildProgressCall = navBuildProgress.InvokeFunc;
        numWaypointsCall = pathNumWaypoints.InvokeFunc;
        listWaypointsCall = pathListWaypoints.InvokeFunc;
        getToleranceCall = pathGetTolerance.InvokeFunc;
        stopCall = pathStop.InvokeAction;
    }

    public bool IsReady()
    {
        return IpcGate.Invoke(navIsReady.HasFunction, isReadyCall, fallback: true, IsReadyFailed);
    }

    public float BuildProgress()
    {
        return IpcGate.Invoke(navBuildProgress.HasFunction, buildProgressCall, BuildIdle, BuildProgressFailed);
    }

    public bool IsRunning()
    {
        return IpcGate.Invoke(pathIsRunning.HasFunction, isRunningCall, fallback: false, IsRunningFailed);
    }

    public bool IsPathfinding()
    {
        if (!IpcGate.Invoke(simpleMovePathfindInProgress.HasFunction, simpleMovePathfindInProgressCall, fallback: false, SimpleMovePathfindFailed))
        {
            return IpcGate.Invoke(navPathfindInProgress.HasFunction, navPathfindInProgressCall, fallback: false, NavPathfindFailed);
        }
        return true;
    }

    public bool IsBusy()
    {
        if (!IsRunning())
        {
            return IsPathfinding();
        }
        return true;
    }

    public Vector3? NearestPoint(Vector3 position, float halfExtentXZ = 5f, float halfExtentY = 5f)
    {
        return IpcGate.Invoke(nearestPoint.HasFunction, () => nearestPoint.InvokeFunc(position, halfExtentXZ, halfExtentY), null, NearestPointFailed);
    }

    public Vector3? NearestPointReachable(Vector3 position, float halfExtentXZ = 5f, float halfExtentY = 5f)
    {
        return IpcGate.Invoke(nearestPointReachable.HasFunction, () => nearestPointReachable.InvokeFunc(position, halfExtentXZ, halfExtentY), null, NearestPointReachableFailed);
    }

    public Vector3? NearestStandablePoint(Vector3 position, float halfExtentXZ = 5f, float halfExtentY = 5f)
    {
        return NearestPointReachable(position, halfExtentXZ, halfExtentY) ?? NearestPoint(position, halfExtentXZ, halfExtentY);
    }

    public Vector3? PointOnFloor(Vector3 point, bool allowUnlandable, float halfExtentXZ)
    {
        return IpcGate.Invoke(pointOnFloor.HasFunction, () => pointOnFloor.InvokeFunc(point, allowUnlandable, halfExtentXZ), null, PointOnFloorFailed);
    }

    public Vector3? HighestFloor(Vector3 point, bool allowUnlandable, float halfExtentXZ)
    {
        return PointOnFloor(point with { Y = AboveEveryTerrainY }, allowUnlandable, halfExtentXZ);
    }

    public int NumWaypoints()
    {
        return IpcGate.Invoke(pathNumWaypoints.HasFunction, numWaypointsCall, WaypointsUnavailable, NumWaypointsFailed);
    }

    public Vector3? CurrentWaypoint()
    {
        List<Vector3>? waypoints = IpcGate.Invoke(pathListWaypoints.HasFunction, listWaypointsCall, null, ListWaypointsFailed);
        if (waypoints == null || waypoints.Count <= 0)
        {
            return null;
        }
        return waypoints[0];
    }

    public void Stop()
    {
        IpcGate.Run(pathStop.HasAction, stopCall, StopFailed);
    }

    public float Tolerance()
    {
        return IpcGate.Invoke(pathGetTolerance.HasFunction, getToleranceCall, NoTolerance, GetToleranceFailed);
    }

    public bool PathfindAndMoveCloseTo(Vector3 destination, bool fly, float arriveWithin)
    {
        return IpcGate.Invoke(simpleMovePathfindAndMoveCloseTo.HasFunction, () => simpleMovePathfindAndMoveCloseTo.InvokeFunc(destination, fly, arriveWithin), fallback: false, PathfindAndMoveCloseToFailed);
    }

    public void MoveAlong(List<Vector3> waypoints, bool fly)
    {
        IpcGate.Run(pathMoveTo.HasAction, () =>
        {
            pathMoveTo.InvokeAction(waypoints, fly);
        }, MoveToFailed);
    }
}
