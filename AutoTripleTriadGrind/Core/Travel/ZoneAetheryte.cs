using System.Numerics;

namespace AutoTripleTriadGrind.Core.Travel;

internal readonly record struct ZoneAetheryte(uint Id, string Name, Vector3 Position);

internal readonly record struct ZoneGateway(uint AetheryteId, uint TerritoryId, string Name, Vector3 Position);

internal readonly record struct AethernetShortcut(ZoneAetheryte Source, ZoneAetheryte Destination, float SavedMeters);
