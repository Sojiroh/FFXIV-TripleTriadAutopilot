using System.Text;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Triad.Data;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Debug;

internal static class TriadNpcDumper
{
    public static void Dump()
    {
        TriadData.EnsureLoaded();
        if (!TriadData.Loaded)
        {
            Svc.Chat.PrintError($"{AttgConstants.LogPrefix} Triple Triad data is not loaded: " + TriadData.FailureReason);
            return;
        }
        TriadOwnership.Refresh(force: true);
        var set = TriadData.Set;
        var configuration = Plugin.Instance.Configuration;
        var builder = new StringBuilder(65536);
        for (int i = 0; i < set.NpcCount; i++)
        {
            var npc = set.Npcs[i];
            builder.Clear();
            builder.Append("NPC ").Append(i).Append(" row=")
                .Append(npc.TriadRowId)
                .Append(" enpc=")
                .Append(npc.ENpcBaseId)
                .Append(" '")
                .Append(set.NpcNames[i])
                .Append("' territory=")
                .Append(npc.TerritoryId)
                .Append(" pos=")
                .Append(npc.Position)
                .Append(" fee=")
                .Append(npc.Fee)
                .Append(" rules=0x")
                .Append(npc.RuleMask.ToString("X4"))
                .Append(" eligibility=")
                .Append(NpcEligibility.Check(i, configuration))
                .Append(" beaten=")
                .Append(TriadOwnership.IsNpcBeaten(npc.TriadRowId))
                .Append(" rewards=[");
            var rewards = set.RewardsOf(i);
            for (int j = 0; j < rewards.Length; j++)
            {
                var cardId = rewards[j];
                builder.Append((j == 0) ? string.Empty : ", ").Append(set.CardNames[cardId]).Append(TriadOwnership.IsOwned(cardId) ? " (owned)" : string.Empty);
            }
            builder.Append(']');
            RunLog.Info(builder.ToString());
        }
        Svc.Chat.Print($"{AttgConstants.LogPrefix} Wrote {set.NpcCount} Triple Triad NPCs to the plugin log; you own {TriadOwnership.OwnedCount} of {set.CardCount} cards.");
    }
}
