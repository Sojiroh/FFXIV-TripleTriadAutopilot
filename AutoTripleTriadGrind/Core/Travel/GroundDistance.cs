using System.Numerics;
using ECommons.MathHelpers;

namespace AutoTripleTriadGrind.Core.Travel;

internal static class GroundDistance
{
    public static float Between(Vector3 from, Vector3 to)
    {
        return Vector2.Distance(from.ToVector2(), to.ToVector2());
    }

    public static float SquaredBetween(Vector3 from, Vector3 to)
    {
        return Vector2.DistanceSquared(from.ToVector2(), to.ToVector2());
    }
}
