using System.Numerics;
using AutoTripleTriadGrind.Core.External;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Pages;

internal sealed class PluginsPage
{
    private const float PadX = 16f;

    private const float DiscRadius = 17f;

    private const float InstallButtonHeight = 30f;

    public void Draw()
    {
        var plugins = ExternalPlugins.All;
        var missingRequired = 0;
        for (var i = 0; i < plugins.Count; i++)
        {
            if (ExternalPlugins.Catalog[plugins[i]].Required && !ExternalPlugins.IsInstalled(plugins[i]))
            {
                missingRequired++;
            }
        }
        var subtitle = (missingRequired == 0) ? Loc.T(L.Plugins.AllInstalled) : Loc.Plural(L.Plugins.Missing, missingRequired);
        PageHeader.Draw(Loc.T(L.Plugins.Title), subtitle, (missingRequired == 0) ? Styling.AccentMint : Styling.AccentRose);
        for (var j = 0; j < plugins.Count; j++)
        {
            DrawCard(plugins[j]);
            Styling.VSpace(4f);
        }
        Styling.VSpace(6f);
        using (Fonts.PushCaption())
        {
            var cursorScreenPos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var footer = Loc.T(L.Plugins.Footer);
            TextDraw.Wrapped(footer, cursorScreenPos, availWidth, Styling.TextMuted);
            ImGui.Dummy(new Vector2(availWidth, TextDraw.MeasureWrapped(footer, availWidth).Y));
        }
    }

    private static string Purpose(ExternalPlugin plugin)
    {
        return plugin switch
        {
            ExternalPlugin.Vnavmesh => Loc.T(L.Plugins.PurposeVnavmesh),
            _ => ExternalPlugins.Catalog[plugin].Purpose,
        };
    }

    private static void DrawCard(ExternalPlugin plugin)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var info = ExternalPlugins.Catalog[plugin];
        var installed = ExternalPlugins.IsInstalled(plugin);
        var installing = PluginInstaller.IsInstalling(plugin);
        var (statusIcon, statusColor) = (installed, info.Required) switch
        {
            (true, _) => (FontAwesomeIcon.CheckCircle, Styling.AccentMint),
            (_, true) => (FontAwesomeIcon.TimesCircle, Styling.AccentRose),
            _ => (FontAwesomeIcon.Circle, Styling.TextDim),
        };
        var cardSize = new Vector2(ImGui.GetContentRegionAvail().X, 76f * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var cardMax = cursorScreenPos + cardSize;
        var windowDrawList = ImGui.GetWindowDrawList();
        Paint.Glass(windowDrawList, cursorScreenPos, cardMax, 10f * globalScale, statusColor, 0.06f);
        var padX = PadX * globalScale;
        var midY = cursorScreenPos.Y + cardSize.Y * 0.5f;
        var discRadius = DiscRadius * globalScale;
        var center = new Vector2(cursorScreenPos.X + padX + discRadius, midY);
        ProgressRing.Disc(center, discRadius, Styling.Tint(Styling.Surface1, statusColor, 0.3f));
        ProgressRing.Track(center, discRadius, 1.2f * globalScale, Styling.WithAlpha(statusColor, 0.7f));
        ProgressRing.CenterIcon(center, statusIcon, statusColor, discRadius * 0.95f);
        var actionWidth = DrawAction(plugin, installed, installing, cardMax, midY);
        var purpose = Purpose(plugin);
        var textX = center.X + discRadius + 16f * globalScale;
        var maxWidth = cardMax.X - padX - actionWidth - textX;
        float nameHeight;
        using (Fonts.PushHeadline())
        {
            nameHeight = TextDraw.Measure(info.DisplayName).Y;
        }
        float purposeHeight;
        using (Fonts.PushCaption())
        {
            purposeHeight = TextDraw.Measure(purpose).Y;
        }
        var textTop = midY - (nameHeight + 3f * globalScale + purposeHeight) * 0.5f;
        Vector2 nameSize;
        using (Fonts.PushHeadline())
        {
            nameSize = TextDraw.Measure(info.DisplayName);
            TextDraw.At(info.DisplayName, new Vector2(textX, textTop), Styling.TextStrong);
        }
        DrawRequirementTag(windowDrawList, info.Required, textX + nameSize.X + 10f * globalScale, textTop + nameHeight * 0.5f);
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(purpose, maxWidth), new Vector2(textX, textTop + nameHeight + 3f * globalScale), Styling.TextDim);
        }
        var nameMin = new Vector2(textX, textTop);
        if (Hit.HoveringRect(nameMin, nameMin + nameSize))
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            Tooltip.Show(Loc.T(L.Plugins.RepoHint, info.RepoUrl));
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                UrlActions.OpenInBrowser(info.RepoUrl);
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText(info.RepoUrl);
            }
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(cardSize);
    }

    private static void DrawRequirementTag(ImDrawListPtr drawList, bool required, float x, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var tag = Loc.Upper(required ? Loc.T(L.Plugins.Required) : Loc.T(L.Plugins.Optional));
        using (Fonts.PushCaption())
        {
            var textSize = TextDraw.Measure(tag);
            var min = new Vector2(x, midY - textSize.Y * 0.5f - 3f * globalScale);
            var max = min + textSize + new Vector2(14f * globalScale, 6f * globalScale);
            var color = required ? Styling.AccentGlow : Styling.TextDim;
            Paint.Pill(drawList, min, max, Styling.WithAlpha(color, 0.18f), Styling.WithAlpha(color, 0.45f));
            TextDraw.Middle(tag, min, max, required ? Styling.AccentGlowSoft : Styling.TextSecondary);
        }
    }

    private static float DrawAction(ExternalPlugin plugin, bool installed, bool installing, Vector2 end, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var padX = PadX * globalScale;
        if (installed)
        {
            var installedLabel = Loc.T(L.Plugins.Installed);
            var textSize = TextDraw.Measure(installedLabel);
            var checkSize = TextDraw.IconSize(FontAwesomeIcon.Check);
            var textX = end.X - padX - textSize.X;
            TextDraw.At(installedLabel, new Vector2(textX, midY - textSize.Y * 0.5f), Styling.AccentMint);
            var checkX = textX - 6f * globalScale - checkSize.X;
            TextDraw.Icon(FontAwesomeIcon.Check, new Vector2(checkX, midY - checkSize.Y * 0.5f), Styling.AccentMint);
            return end.X - padX - checkX + 12f * globalScale;
        }
        var label = installing ? Loc.T(L.Plugins.Installing) : Loc.T(L.Plugins.Install);
        var buttonWidth = PillButton.Width(label, FontAwesomeIcon.Download);
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonWidth, midY - InstallButtonHeight * globalScale * 0.5f));
        ImGui.PushID((nint)(plugin + 1));
        if (PillButton.Draw("##install", label, Styling.AccentGlow, PillButton.Emphasis.Filled, FontAwesomeIcon.Download, !installing, InstallButtonHeight))
        {
            _ = PluginInstaller.Install(plugin);
        }
        ImGui.PopID();
        return buttonWidth + 12f * globalScale;
    }
}
