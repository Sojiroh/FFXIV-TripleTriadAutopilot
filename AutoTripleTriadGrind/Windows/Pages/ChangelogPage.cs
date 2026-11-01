using System;
using System.Globalization;
using System.Numerics;
using AutoTripleTriadGrind.Core.Changelog;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Pages;

internal sealed class ChangelogPage
{
    private const float RailWidth = 26f;

    private const float RailDotRadius = 5f;

    private const float CardGap = 14f;

    private const float CardPadX = 18f;

    private const float CardPadY = 16f;

    private const float HeaderGap = 10f;

    private const float SeparatorGap = 12f;

    private const float BulletColumn = 18f;

    private const float BulletRadius = 2.5f;

    private const float BulletGap = 8f;

    private const float PillPadX = 8f;

    private const float PillPadY = 3f;

    private const float PillGap = 10f;

    private const float RevealMs = 360f;

    private const float RevealStaggerMs = 80f;

    private const float RevealSlide = 10f;

    private const string DateFormat = "d MMM yyyy";

    private const string MetaSeparator = " · ";

    private readonly string[] versionLabels = new string[ChangelogData.Entries.Length];

    private readonly string[] metaLabels = new string[ChangelogData.Entries.Length];

    private LanguageInfo? labelsLanguage;

    private long visitTick = long.MinValue;

    private int unseenAtVisit;

    public void Draw(Configuration configuration, long shownTick)
    {
        BeginVisit(configuration, shownTick);
        RefreshLabels();
        PageHeader.Draw(Loc.T(L.Changelog.Title), Loc.T(L.Changelog.Subtitle));
        var entries = ChangelogData.Entries;
        for (var i = 0; i < entries.Length; i++)
        {
            using (Motion.PushReveal(Motion.Reveal(shownTick, RevealMs, i * RevealStaggerMs)))
            {
                DrawEntry(i, i == entries.Length - 1);
            }
        }
    }

    private void BeginVisit(Configuration configuration, long shownTick)
    {
        if (visitTick == shownTick)
        {
            return;
        }
        visitTick = shownTick;
        unseenAtVisit = ChangelogData.UnseenCount(configuration.LastSeenChangelogVersion);
        configuration.MarkChangelogSeen();
    }

    private void RefreshLabels()
    {
        if (labelsLanguage == Loc.Current)
        {
            return;
        }
        labelsLanguage = Loc.Current;
        var entries = ChangelogData.Entries;
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            versionLabels[i] = Loc.T(L.Changelog.Version, entry.Version);
            metaLabels[i] = FormatDate(entry.Date) + MetaSeparator + Loc.Plural(L.Changelog.Changes, entry.Highlights.Length);
        }
    }

    private void DrawEntry(int index, bool isLast)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var entry = ChangelogData.Entries[index];
        var isUnseen = index < unseenAtVisit;
        var isLatest = index == 0;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var availWidth = ImGui.GetContentRegionAvail().X;
        var railWidth = RailWidth * globalScale;
        var min = new Vector2(cursorScreenPos.X + railWidth, cursorScreenPos.Y);
        var cardWidth = availWidth - railWidth;
        var padX = CardPadX * globalScale;
        var padY = CardPadY * globalScale;
        var textLeft = min.X + padX + BulletColumn * globalScale;
        var textWidth = MathF.Max(1f, min.X + cardWidth - padX - textLeft);
        float headlineHeight;
        using (Fonts.PushHeadline())
        {
            headlineHeight = TextDraw.LineHeight();
        }
        var bodyHeight = MeasureBody(entry.Highlights, textWidth);
        var separatorY = min.Y + padY + headlineHeight + SeparatorGap * globalScale;
        var cardHeight = separatorY - min.Y + SeparatorGap * globalScale + bodyHeight + padY;
        var max = new Vector2(min.X + cardWidth, min.Y + cardHeight);
        var windowDrawList = ImGui.GetWindowDrawList();
        var rounding = 10f * globalScale;
        var headerMidY = min.Y + padY + headlineHeight * 0.5f;
        DrawRail(windowDrawList, cursorScreenPos, headerMidY, cardHeight, isUnseen | isLatest, isLast);
        if (isUnseen)
        {
            Paint.Glow(windowDrawList, min, max, rounding, Styling.AccentGlow, 0.8f);
        }
        if (isLatest)
        {
            Paint.Glass(windowDrawList, min, max, rounding, Styling.AccentGlow, 0.08f);
            Paint.Stroke(windowDrawList, min, max, Styling.WithAlpha(Styling.AccentGlow, isUnseen ? 0.55f : 0.3f), rounding);
        }
        else
        {
            Paint.Surface(windowDrawList, min, max, rounding, Styling.WithAlpha(Styling.Surface1, 0.75f), Styling.WithAlpha(Styling.BorderDim, 0.5f));
        }
        var cursorX = min.X + padX;
        using (Fonts.PushHeadline())
        {
            TextDraw.At(versionLabels[index], new Vector2(cursorX, min.Y + padY), isLatest ? Styling.AccentGlowSoft : Styling.TextStrong);
            cursorX += TextDraw.Measure(versionLabels[index]).X + HeaderGap * globalScale;
        }
        if (isUnseen | isLatest)
        {
            cursorX += DrawPill(windowDrawList, isUnseen ? L.Changelog.New : L.Changelog.Latest, cursorX, headerMidY, isUnseen) + PillGap * globalScale;
        }
        using (Fonts.PushCaption())
        {
            var meta = metaLabels[index];
            var metaSize = TextDraw.Measure(meta);
            var metaX = max.X - padX - metaSize.X;
            if (metaX > cursorX)
            {
                TextDraw.At(meta, new Vector2(metaX, headerMidY - metaSize.Y * 0.5f), Styling.TextMuted);
            }
        }
        Paint.Hairline(windowDrawList, new Vector2(min.X + padX, separatorY), new Vector2(max.X - padX, separatorY));
        DrawBody(windowDrawList, entry.Highlights, min.X + padX, textLeft, textWidth, separatorY + SeparatorGap * globalScale);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(availWidth, cardHeight + (isLast ? 0f : (CardGap * globalScale))));
    }

    private static void DrawRail(ImDrawListPtr dl, Vector2 origin, float dotY, float cardHeight, bool highlighted, bool isLast)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var dotCenter = new Vector2(origin.X + RailWidth * globalScale * 0.5f - 2f * globalScale, dotY);
        var dotRadius = RailDotRadius * globalScale;
        if (!isLast)
        {
            var lineEnd = origin.Y + cardHeight + CardGap * globalScale + (dotY - origin.Y) - dotRadius;
            dl.AddLine(dotCenter + new Vector2(0f, dotRadius + 2f * globalScale), new Vector2(dotCenter.X, lineEnd), Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.8f)), 1.5f * globalScale);
        }
        if (highlighted)
        {
            Paint.Dot(dl, dotCenter, dotRadius, Styling.AccentGlow, 0.28f);
        }
        else
        {
            dl.AddCircle(dotCenter, dotRadius, Paint.Col(Styling.WithAlpha(Styling.TextDim, 0.8f)), 0, 1.5f * globalScale);
        }
    }

    private static float DrawPill(ImDrawListPtr dl, LocString label, float x, float midY, bool filled)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        using (Fonts.PushCaption())
        {
            var caption = TextDraw.Upper(Loc.T(label));
            var textSize = TextDraw.Measure(caption);
            var min = new Vector2(x, midY - textSize.Y * 0.5f - PillPadY * globalScale);
            var max = new Vector2(x + textSize.X + 16f * globalScale, midY + textSize.Y * 0.5f + PillPadY * globalScale);
            if (filled)
            {
                Paint.Pill(dl, min, max, Styling.AccentGlow, Styling.WithAlpha(Styling.AccentGlowSoft, 0.6f));
                TextDraw.At(caption, new Vector2(min.X + PillPadX * globalScale, midY - textSize.Y * 0.5f), Styling.ForegroundOn(Styling.AccentGlow));
            }
            else
            {
                Paint.Pill(dl, min, max, Styling.WithAlpha(Styling.AccentGlow, 0.16f), Styling.WithAlpha(Styling.AccentGlow, 0.4f));
                TextDraw.At(caption, new Vector2(min.X + PillPadX * globalScale, midY - textSize.Y * 0.5f), Styling.AccentGlowSoft);
            }
            return max.X - min.X;
        }
    }

    private static float MeasureBody(LocString[] highlights, float textWidth)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = 0f;
        for (var i = 0; i < highlights.Length; i++)
        {
            if (i > 0)
            {
                height += BulletGap * globalScale;
            }
            height += TextDraw.MeasureWrapped(Loc.T(highlights[i]), textWidth).Y;
        }
        return height;
    }

    private static void DrawBody(ImDrawListPtr dl, LocString[] highlights, float bulletX, float textLeft, float textWidth, float top)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var lineHeight = TextDraw.LineHeight();
        var y = top;
        for (var i = 0; i < highlights.Length; i++)
        {
            if (i > 0)
            {
                y += BulletGap * globalScale;
            }
            var line = Loc.T(highlights[i]);
            dl.AddCircleFilled(new Vector2(bulletX + BulletRadius * globalScale, y + lineHeight * 0.5f), BulletRadius * globalScale, Paint.Col(Styling.AccentGlow));
            TextDraw.Wrapped(line, new Vector2(textLeft, y), textWidth, Styling.TextSecondary);
            y += TextDraw.MeasureWrapped(line, textWidth).Y;
        }
    }

    private static string FormatDate(string isoDate)
    {
        if (!DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return isoDate;
        }
        return date.ToString(DateFormat, Loc.Culture);
    }
}
