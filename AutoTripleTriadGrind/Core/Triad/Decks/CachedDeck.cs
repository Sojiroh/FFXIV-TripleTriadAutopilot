using System;
using System.Threading.Tasks;

namespace AutoTripleTriadGrind.Core.Triad.Decks;

internal static class DeckBuildReuse
{
    public static bool Serves(Task? build, uint builtForNpcRow, ushort builtForRules, uint npcRow, ushort ruleMask) => build != null && !build.IsFaulted && !build.IsCanceled && builtForNpcRow == npcRow && builtForRules == ruleMask;
}

public sealed class CachedDeck
{
    public ushort[] Cards { get; set; } = Array.Empty<ushort>();

    public float WinChance { get; set; }

    public int OwnedCount { get; set; }

    public ushort RuleMask { get; set; }
}
