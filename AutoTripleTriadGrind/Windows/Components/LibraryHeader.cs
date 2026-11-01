using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class LibraryHeader
{
    public static void Draw(string label, bool scrollIntoView)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = Layout.LibraryHeaderHeight * globalScale;
        var titleSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, new Vector2(cursorScreenPos.X, cursorScreenPos.Y + (height - titleSize.Y) * 0.5f), Styling.TextStrong);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(width, height));
        if (scrollIntoView)
        {
            ImGui.SetScrollHereY(0f);
        }
    }
}
