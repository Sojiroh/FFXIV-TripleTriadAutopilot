using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoTripleTriadGrind.Core.Triad.Data;

internal static class TriadUnlock
{
    public const int NoMissingQuest = -1;

    public static bool IsUnlocked(int npcIndex)
    {
        return MissingQuestSlot(npcIndex) == NoMissingQuest || TriadOwnership.IsNpcBeaten(TriadData.Set.Npcs[npcIndex].TriadRowId);
    }

    public static int MissingQuestSlot(int npcIndex)
    {
        var set = TriadData.Set;
        var unlockQuests = set.UnlockQuestsOf(npcIndex);
        var unlockNeedsAnyQuest = set.Npcs[npcIndex].UnlockNeedsAnyQuest;
        var missingSlot = NoMissingQuest;
        for (var i = 0; i < unlockQuests.Length; i++)
        {
            if (unlockQuests[i] == 0)
            {
                continue;
            }
            if (IsQuestDone(unlockQuests[i]))
            {
                if (unlockNeedsAnyQuest)
                {
                    return NoMissingQuest;
                }
            }
            else if (missingSlot == NoMissingQuest)
            {
                missingSlot = i;
            }
        }
        return missingSlot;
    }

    private unsafe static bool IsQuestDone(uint questRowId)
    {
        if (QuestManager.IsQuestComplete(questRowId) || QuestManager.IsQuestComplete((ushort)(questRowId & 0xFFFF)))
        {
            return true;
        }
        var uiState = UIState.Instance();
        return uiState != null && uiState->IsUnlockLinkUnlockedOrQuestCompleted(questRowId, 0);
    }
}
