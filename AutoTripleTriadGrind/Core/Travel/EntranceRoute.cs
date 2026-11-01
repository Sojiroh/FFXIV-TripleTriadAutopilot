using System.Numerics;

namespace AutoTripleTriadGrind.Core.Travel;

internal readonly record struct EntranceRoute(uint SourceTerritoryId, uint TargetTerritoryId, Vector3 Position, uint EntranceBaseId);
