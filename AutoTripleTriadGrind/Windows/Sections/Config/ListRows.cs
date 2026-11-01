using System;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Sections.Config;

internal static class ListRows
{
    private static string[] ordinals = Array.Empty<string>();

    public static void Ordinal(int index)
    {
        if (index >= ordinals.Length)
        {
            GrowOrdinals(index + 1);
        }
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
        {
            ImGui.TextUnformatted(ordinals[index]);
        }
    }

    public static bool RemoveButton(string listId, int index, string tooltip)
    {
        var frameHeight = ImGui.GetFrameHeight();
        ImGui.SameLine(SettingsGroup.InnerRightLocalX() - frameHeight);
        ImGui.PushID(listId);
        ImGui.PushID(index);
        var removed = IconButton.Draw(FontAwesomeIcon.Times, "##remove", frameHeight, Styling.AccentRose, tooltip);
        ImGui.PopID();
        ImGui.PopID();
        return removed;
    }

    private static void GrowOrdinals(int count)
    {
        var grown = new string[Math.Max(count, ordinals.Length * 2)];
        Array.Copy(ordinals, grown, ordinals.Length);
        for (var i = ordinals.Length; i < grown.Length; i++)
        {
            grown[i] = $"{i + 1}.";
        }
        ordinals = grown;
    }
}
