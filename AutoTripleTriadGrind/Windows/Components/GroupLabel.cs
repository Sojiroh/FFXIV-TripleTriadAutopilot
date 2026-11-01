using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class GroupLabel
{
    public static void Draw(string label, float spaceBefore = 0f, float extraBelow = 0f)
    {
        if (spaceBefore > 0f)
        {
            Styling.VSpace(spaceBefore);
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(cursorScreenPos.X + 2f * globalScale, cursorScreenPos.Y), Styling.TextMuted);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, labelSize.Y + extraBelow * globalScale));
    }
}
