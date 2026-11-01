using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace AutoTripleTriadGrind.Core.Game.Ops;

internal static class CardRegistrar
{
    private static readonly InventoryType[] bags = new InventoryType[4]
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4
    };

    public unsafe static int FreeBagSlots()
    {
        var inventory = InventoryManager.Instance();
        if (inventory != null)
        {
            return (int)inventory->GetEmptySlotsInBag();
        }
        return 0;
    }

    public unsafe static int Mgp()
    {
        var inventory = InventoryManager.Instance();
        if (inventory != null)
        {
            return (int)inventory->GetGoldSaucerCoin();
        }
        return 0;
    }

    public unsafe static uint FindUnregisteredCardItem(IReadOnlySet<uint> skipped)
    {
        var inventory = InventoryManager.Instance();
        if (inventory == null)
        {
            return 0u;
        }
        var cardIdByItemId = TriadData.Set.CardIdByItemId;
        for (int i = 0; i < bags.Length; i++)
        {
            var bag = inventory->GetInventoryContainer(bags[i]);
            if (bag == null)
            {
                continue;
            }
            for (int j = 0; j < bag->Size; j++)
            {
                var slot = bag->GetInventorySlot(j);
                if (slot != null && slot->ItemId != 0 && cardIdByItemId.TryGetValue(slot->ItemId, out var cardId) && !TriadOwnership.IsOwned(cardId) && !skipped.Contains(slot->ItemId))
                {
                    return slot->ItemId;
                }
            }
        }
        return 0u;
    }

    public static bool IsCollected(ushort cardId)
    {
        if (!TriadOwnership.IsOwned(cardId))
        {
            return HoldsCardItem(cardId);
        }
        return true;
    }

    private unsafe static bool HoldsCardItem(ushort cardId)
    {
        var cardItemIds = TriadData.Set.CardItemIds;
        if (cardId >= cardItemIds.Length || cardItemIds[cardId] == 0)
        {
            return false;
        }
        var inventory = InventoryManager.Instance();
        if (inventory != null)
        {
            return inventory->GetInventoryItemCount(cardItemIds[cardId], isHq: false, checkEquipped: false, checkArmory: false, 0) > 0;
        }
        return false;
    }

    public unsafe static bool Use(uint itemId)
    {
        var context = AgentInventoryContext.Instance();
        if (context == null)
        {
            return false;
        }
        context->UseItem(itemId, InventoryType.Invalid, 0u, 0);
        return true;
    }
}
