using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

[StructLayout(LayoutKind.Explicit, Size = 464)]
internal struct TriadAgent
{
    [FieldOffset(0)]
    public AgentInterface AgentInterface;

    [FieldOffset(456)]
    public uint RewardItemId;

    public unsafe static uint ReadRewardItemId()
    {
        var agentModule = AgentModule.Instance();
        if (agentModule == null)
        {
            return 0u;
        }
        var triadAgent = (TriadAgent*)agentModule->GetAgentByInternalId(AgentId.TripleTriad);
        if (triadAgent != null)
        {
            return triadAgent->RewardItemId;
        }
        return 0u;
    }
}
