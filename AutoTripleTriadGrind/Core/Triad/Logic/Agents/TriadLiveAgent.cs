namespace AutoTripleTriadGrind.Core.Triad.Logic.Agents;

public sealed class TriadLiveAgent : TriadRolloutAgent
{
    public const long MaxStatesToExplore = 10000L;

    private int minPlacedToExplore = 10;

    private int minPlacedToExploreWithForced = 10;

    public override void Initialize(int sessionSeed)
    {
        base.Initialize(sessionSeed);
        var forcedStates = 1L;
        var freeStates = 1L;
        for (var i = 1; i <= TriadGameState.BoardCells; i++)
        {
            var placed = TriadGameState.BoardCells - i;
            forcedStates *= i;
            if (forcedStates <= MaxStatesToExplore)
            {
                minPlacedToExploreWithForced = placed;
            }
            freeStates *= i * ((i + 2) / 2) * ((i + 1) / 2);
            if (freeStates <= MaxStatesToExplore)
            {
                minPlacedToExplore = placed;
            }
        }
    }

    protected override bool CanRunRandomExploration(TriadGameState state, int searchLevel)
    {
        var minPlaced = ((state.ForcedCardIndex < 0) ? minPlacedToExplore : minPlacedToExploreWithForced);
        return searchLevel > 0 && state.NumCardsPlaced < minPlaced;
    }
}
