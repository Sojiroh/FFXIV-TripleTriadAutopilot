using System;
using System.Text;
using AutoTripleTriadGrind.Core.Triad.Addons;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Debug;

internal static class TriadBoardDumper
{
    public static void DumpBoard()
    {
        TriadData.EnsureLoaded();
        var screen = new TriadScreenState();
        if (!TriadMatchOps.TryReadBoard(screen, out var frame))
        {
            Svc.Chat.PrintError($"{AttgConstants.LogPrefix} No Triple Triad board is open.");
            return;
        }
        var builder = new StringBuilder(512);
        builder.Append("Board: canAct=").Append(frame.CanAct).Append(" playerTurn=")
            .Append(frame.PlayerTurn)
            .Append(" turnState=")
            .Append(frame.TurnState)
            .Append(" banner=")
            .Append(frame.BannerVisible)
            .Append(" readFailed=")
            .Append(frame.ReadFailed)
            .Append(" pvp=")
            .Append(frame.IsPvP);
        Report(builder);
        Report(builder.Append("Rules: ").Append(RuleList(screen)));
        Report(builder.Append("Blue: ").Append(Hand(screen.BlueHand)).Append(" forced=")
            .Append(CardLabel(screen.ForcedBlueCard)));
        Report(builder.Append("Red: ").Append(Hand(screen.RedHand)));
        for (int i = 0; i < 3; i++)
        {
            builder.Append("Row ").Append(i).Append(':');
            for (int j = 0; j < 3; j++)
            {
                var cell = i * 3 + j;
                builder.Append(' ').Append(CardLabel(screen.Board[cell])).Append('/')
                    .Append(screen.BoardOwners[cell]);
            }
            Report(builder);
        }
    }

    public static void DumpRequest()
    {
        TriadData.EnsureLoaded();
        Span<TriadRuleId> regional = stackalloc TriadRuleId[2];
        if (!TriadMatchOps.TryReadRegionalRules(regional))
        {
            Svc.Chat.PrintError($"{AttgConstants.LogPrefix} No Triple Triad challenge window is open, or its layout was not recognised.");
            return;
        }
        Svc.Chat.Print($"{AttgConstants.LogPrefix} Regional rules: {regional[0]}, {regional[1]}");
    }

    private static void Report(StringBuilder builder)
    {
        var line = builder.ToString();
        builder.Clear();
        Svc.Chat.Print(AttgConstants.LogPrefix + " " + line);
        RunLog.Info(line);
    }

    private static string RuleList(TriadScreenState screen)
    {
        if (screen.Rules.Count == 0)
        {
            return "none";
        }
        string joined = string.Empty;
        for (int i = 0; i < screen.Rules.Count; i++)
        {
            var rule = screen.Rules[i];
            var entry = rule is TriadRuleRoulette roulette && roulette.Resolved is { } resolved ? $"Roulette({resolved.Id})" : rule.Id.ToString();
            joined = (i == 0) ? entry : (joined + ", " + entry);
        }
        return joined;
    }

    private static string Hand(ushort[] hand)
    {
        var text = string.Empty;
        for (int i = 0; i < hand.Length; i++)
        {
            text = ((i == 0) ? CardLabel(hand[i]) : (text + ", " + CardLabel(hand[i])));
        }
        return text;
    }

    private static string CardLabel(ushort cardId)
    {
        return cardId switch
        {
            ushort.MaxValue => "-",
            0 => "?",
            _ => TriadData.Set.CardName(cardId),
        };
    }
}
