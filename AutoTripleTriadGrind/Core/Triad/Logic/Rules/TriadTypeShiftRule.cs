using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Core.Triad.Logic.Rules;

public abstract class TriadTypeShiftRule(TriadRuleId id, int step, float scoreMultiplier, bool favorsTyped) : TriadRule(id, TriadRuleFeatures.CardPlaced | TriadRuleFeatures.PostCapture)
{
    private readonly int step = step;

    private readonly float scoreMultiplier = scoreMultiplier;

    private readonly bool favorsTyped = favorsTyped;

    public override void OnCardPlaced(TriadGameState state, int boardPosition)
    {
        ref var slot = ref state.Board[boardPosition];
        var type = slot.Type;
        if (type == TriadCardType.None)
        {
            return;
        }
        var modifier = state.TypeModifier(type);
        if (modifier != 0)
        {
            slot.ScoreModifier = (sbyte)modifier;
        }
    }

    public override void OnPostCaptures(TriadGameState state, int boardPosition)
    {
        var board = state.Board;
        var type = board[boardPosition].Type;
        if (type == TriadCardType.None)
        {
            return;
        }
        var modifier = board[boardPosition].ScoreModifier + step;
        state.TypeModifiers[(uint)type] = modifier;
        for (var i = 0; i < board.Length; i++)
        {
            if (!board[i].IsEmpty && board[i].Type == type)
            {
                board[i].ScoreModifier = (sbyte)modifier;
            }
        }
    }

    public override void OnScreenUpdate(TriadGameState state)
    {
        var board = state.Board;
        state.ClearTypeModifiers();
        for (var i = 0; i < board.Length; i++)
        {
            if (!board[i].IsEmpty && board[i].Type != TriadCardType.None)
            {
                state.TypeModifiers[(uint)board[i].Type] += step;
            }
        }
        for (var j = 0; j < board.Length; j++)
        {
            if (!board[j].IsEmpty && board[j].Type != TriadCardType.None)
            {
                board[j].ScoreModifier = (sbyte)state.TypeModifiers[(uint)board[j].Type];
            }
        }
    }

    public override void OnScoreCard(ushort cardId, ref float score)
    {
        score *= scoreMultiplier;
        if (TriadCards.Get(cardId).Type != TriadCardType.None == favorsTyped)
        {
            score += 1f - scoreMultiplier;
        }
    }
}

public sealed class TriadRuleAscension : TriadTypeShiftRule
{
    public TriadRuleAscension()
        : base(TriadRuleId.Ascension, 1, 0.8f, favorsTyped: true)
    {
    }
}

public sealed class TriadRuleDescension : TriadTypeShiftRule
{
    public TriadRuleDescension()
        : base(TriadRuleId.Descension, -1, 0.5f, favorsTyped: false)
    {
    }
}
