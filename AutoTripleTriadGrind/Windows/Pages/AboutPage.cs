using System;
using System.Numerics;
using AutoTripleTriadGrind.Core;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Windows.Pages;

internal sealed class AboutPage
{
    private readonly record struct CommunityLink(string Id, FontAwesomeIcon Icon, LocString Title, LocString Body, string Url, Vector4 Accent);

    private readonly record struct FactCategory(FontAwesomeIcon Icon, LocString Header, Vector4 Color, LocString[] Lines);

    private const float TileHeight = 96f;

    private const float TileGap = 14f;

    private const float TileStackBelow = 540f;

    private const float TilePad = 18f;

    private const float TileMedallion = 26f;

    private const float TileTextGap = 16f;

    private const float TileLift = 3f;

    private const float ArrowSlide = 5f;

    private static readonly CommunityLink[] communityLinks = new CommunityLink[1]
    {
        new CommunityLink("##tta_about_github", FontAwesomeIcon.CodeBranch, L.About.GitHubTitle, L.About.GitHubBody, RepoUrl, Styling.AccentNebula)
    };

    private const string Name = "Triple Triad Autopilot";

    private const string Author = "Sojiroh";

    private const string RepoUrl = "https://github.com/Sojiroh/FFXIV-TripleTriadAutopilot";

    // Attribution required by the NOTICE of the original project; never translated or altered.
    private const string CreditText = "Based on Auto Triple Triad Grind by XeldarAlz";

    private const string CreditUrl = "https://github.com/XeldarAlz/FFXIV-AutoTripleTriadGrind";

    private static readonly string[] CreditLines = [CreditText, CreditUrl];

    private const float MaxColumnWidth = 860f;

    private const float SectionGap = 22f;

    private const float RevealMs = 460f;

    private const float RevealStaggerMs = 110f;

    private const float RevealSlide = 14f;

    private const float HeroHeight = 184f;

    private const float HeroPad = 26f;

    private const float HeroIconSize = 108f;

    private const float HeroTextGap = 26f;

    private const float HeroLineGap = 6f;

    private const float ChipPadX = 10f;

    private const float ChipPadY = 4f;

    private const float ChipIconGap = 6f;

    private const float FooterGap = 6f;

    private static readonly Vector2[] BloomOffsets = new Vector2[4]
    {
        new Vector2(1.6f, 0f),
        new Vector2(-1.6f, 0f),
        new Vector2(0f, 1.6f),
        new Vector2(0f, -1.6f)
    };

    private static readonly string version = typeof(AboutPage).Assembly.GetName().Version?.ToString() ?? "?";

    private long openTick = -4611686018427387904L;

    private float columnX;

    private float columnWidth;

    private static readonly FactCategory[] Categories = new FactCategory[4]
    {
        new FactCategory(FontAwesomeIcon.Heart, L.About.ReminderTitle, Styling.AccentRose, L.About.Reminders),
        new FactCategory(FontAwesomeIcon.Lightbulb, L.About.FactsTitle, Styling.AccentAmberSoft, L.About.Facts),
        new FactCategory(FontAwesomeIcon.Star, L.About.QuotesTitle, Styling.AccentMintSoft, L.About.Quotes),
        new FactCategory(FontAwesomeIcon.GrinBeam, L.About.JokesTitle, Styling.AccentBlueSoft, L.About.Jokes)
    };

    private static readonly int[][] factBags = new int[Categories.Length][];

    private static readonly int[] factBagPositions = new int[Categories.Length];

    private static readonly int[] factLastServed = new int[Categories.Length];

    private static int factCategory = -1;

    private static int factLine;

    private static bool iconHovered;

    private static string Punchline => Svc.PluginInterface.Manifest.Punchline ?? string.Empty;

    private void DrawCommunity()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        SectionHeader(FontAwesomeIcon.Users, Loc.T(L.About.Community), Styling.AccentNebula, columnWidth);
        var gridOrigin = new Vector2(columnX, ImGui.GetCursorScreenPos().Y);
        var gap = TileGap * globalScale;
        var stacked = columnWidth < TileStackBelow * globalScale;
        var columns = stacked ? 1 : Math.Min(communityLinks.Length, 2);
        var size = new Vector2((columnWidth - gap * (columns - 1)) / columns, TileHeight * globalScale);
        for (var i = 0; i < communityLinks.Length; i++)
        {
            var offset = new Vector2(i % columns * (size.X + gap), i / columns * (size.Y + gap));
            DrawTile(in communityLinks[i], gridOrigin + offset, size);
        }
        var rows = (communityLinks.Length + columns - 1) / columns;
        ImGui.SetCursorScreenPos(gridOrigin);
        ImGui.Dummy(new Vector2(columnWidth, rows * size.Y + (rows - 1) * gap));
    }

    private static void DrawTile(in CommunityLink link, Vector2 slot, Vector2 size)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        ImGui.SetCursorScreenPos(slot);
        var hit = Hit.Area(link.Id, size);
        var hover = Motion.Hover(Motion.Key(link.Id), hit.Hovered);
        var press = Motion.Approach(Motion.Key(link.Id, 1), hit.Held ? 1f : 0f, 30f);
        OpenOrCopy(hit, link.Url);
        var lift = (TileLift * hover - press * 1.5f) * globalScale;
        var min = slot - new Vector2(0f, lift);
        var max = min + size;
        var rounding = 14f * globalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var accent = link.Accent;
        if (hover > 0.01f)
        {
            Paint.Shadow(windowDrawList, min, max, rounding, 12f * globalScale, 0.35f * hover);
            Paint.Glow(windowDrawList, min, max, rounding, accent, hover);
        }
        Paint.Glass(windowDrawList, min, max, rounding, accent, 0.09f + 0.1f * hover);
        Paint.Stroke(windowDrawList, min, max, Styling.WithAlpha(accent, 0.28f + 0.5f * hover), rounding, 1.2f * globalScale);
        var pad = TilePad * globalScale;
        var medallion = TileMedallion * globalScale;
        var center = new Vector2(min.X + pad + medallion, min.Y + size.Y * 0.5f);
        windowDrawList.AddCircleFilled(center, medallion * (1.25f + 0.1f * hover), Paint.Col(Styling.WithAlpha(accent, 0.1f + 0.1f * hover)), 40);
        windowDrawList.AddCircleFilled(center, medallion, Paint.Col(Vector4.Lerp(accent, Styling.Lighten(accent, 0.15f), hover)), 40);
        windowDrawList.AddCircle(center, medallion, Paint.Col(Styling.WithAlpha(Styling.Lighten(accent, 0.5f), 0.55f)), 40, 1.2f * globalScale);
        TextDraw.IconCentered(link.Icon, center, Styling.ForegroundOn(accent), 1.1f + 0.12f * hover);
        var arrowSize = TextDraw.IconSize(FontAwesomeIcon.ArrowRight);
        var arrowX = max.X - pad - arrowSize.X - ArrowSlide * globalScale * (1f - hover);
        TextDraw.Icon(FontAwesomeIcon.ArrowRight, new Vector2(arrowX, min.Y + (size.Y - arrowSize.Y) * 0.5f), Vector4.Lerp(Styling.WithAlpha(Styling.TextDim, 0.7f), Styling.Lighten(accent, 0.35f), hover));
        var textX = center.X + medallion + TileTextGap * globalScale;
        var textWidth = MathF.Max(1f, arrowX - TileTextGap * globalScale - textX);
        var title = Loc.T(link.Title);
        var body = Loc.T(link.Body);
        float titleHeight;
        using (Fonts.PushHeadline())
        {
            titleHeight = TextDraw.LineHeight();
        }
        float bodyHeight;
        using (Fonts.PushCaption())
        {
            bodyHeight = TextDraw.MeasureWrapped(body, textWidth).Y;
        }
        var textTop = min.Y + (size.Y - titleHeight - 4f * globalScale - bodyHeight) * 0.5f;
        using (Fonts.PushHeadline())
        {
            TextDraw.At(TextDraw.Truncate(title, textWidth), new Vector2(textX, textTop), Styling.TextStrong);
        }
        using (Fonts.PushCaption())
        {
            TextDraw.Wrapped(body, new Vector2(textX, textTop + titleHeight + 4f * globalScale), textWidth, Vector4.Lerp(Styling.TextDim, Styling.TextSecondary, hover));
        }
    }

    public void Draw(long shownTick)
    {
        openTick = shownTick;
        var globalScale = ImGuiHelpers.GlobalScale;
        var availWidth = ImGui.GetContentRegionAvail().X;
        columnWidth = MathF.Min(availWidth, MaxColumnWidth * globalScale);
        columnX = ImGui.GetCursorScreenPos().X + (availWidth - columnWidth) * 0.5f;
        Styling.VSpace(6f);
        RevealSection(0, DrawHero);
        Styling.VSpace(SectionGap);
        RevealSection(1, DrawCommunity);
        Styling.VSpace(SectionGap);
        RevealSection(2, DrawFooter);
        Styling.VSpace(SectionGap);
    }

    private void RevealSection(int index, Action draw)
    {
        var elapsed = Environment.TickCount64 - openTick;
        var progress = Motion.Reduced ? 1f : Motion.EaseOutCubic(Math.Clamp((elapsed - index * RevealStaggerMs) / RevealMs, 0f, 1f));
        var slideY = ImGui.GetCursorScreenPos().Y + (1f - progress) * RevealSlide * ImGuiHelpers.GlobalScale;
        ImGui.SetCursorScreenPos(new Vector2(columnX, slideY));
        using (Motion.PushAlpha(progress))
        {
            draw();
        }
    }

    private void DrawHero()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var heroHeight = HeroHeight * globalScale;
        var max = cursorScreenPos + new Vector2(columnWidth, heroHeight);
        var rounding = 16f * globalScale;
        Paint.Shadow(windowDrawList, cursorScreenPos, max, rounding, 16f * globalScale, 0.5f);
        Paint.Gradient(windowDrawList, cursorScreenPos, max, Styling.Tint(Styling.Surface2, Styling.AccentGlow, 0.2f), Styling.Tint(Styling.Surface0, Styling.AccentGlow, 0.05f), rounding);
        DrawAurora(windowDrawList, cursorScreenPos, max);
        Paint.TopLight(windowDrawList, cursorScreenPos, max, rounding, 0.14f);
        Paint.Stroke(windowDrawList, cursorScreenPos, max, Styling.WithAlpha(Styling.AccentGlow, 0.35f), rounding, 1.2f * globalScale);
        var heroPad = HeroPad * globalScale;
        var iconSize = MathF.Min(HeroIconSize * globalScale, heroHeight - heroPad * 2f);
        var textGap = HeroTextGap * globalScale;
        var textWidth = MathF.Min(HeroTextWidth(), MathF.Max(1f, columnWidth - heroPad * 2f - iconSize - textGap));
        var contentX = cursorScreenPos.X + (columnWidth - iconSize - textGap - textWidth) * 0.5f;
        var bob = Motion.Reduced ? 0f : (Motion.Wave(3200.0) * 3f * globalScale);
        var center = new Vector2(contentX + iconSize * 0.5f, cursorScreenPos.Y + heroHeight * 0.5f + bob);
        DrawHeroIcon(windowDrawList, center, iconSize);
        DrawHeroText(contentX + iconSize + textGap, textWidth, cursorScreenPos.Y, heroHeight);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(columnWidth, heroHeight));
    }

    private static void DrawAurora(ImDrawListPtr dl, Vector2 min, Vector2 max)
    {
        var width = max.X - min.X;
        var height = max.Y - min.Y;
        dl.PushClipRect(min, max, intersectWithCurrentClipRect: true);
        SoftBlob(dl, min + new Vector2(width * (0.18f + 0.08f * Motion.Wave(11000.0)), height * (0.3f + 0.2f * Motion.Wave(13700.0))), height * 1.3f, Styling.AccentGlow, 0.1f);
        SoftBlob(dl, min + new Vector2(width * (0.72f + 0.1f * Motion.Wave(15500.0)), height * (0.1f + 0.25f * Motion.Wave(9300.0))), height * 1.1f, Styling.AccentNebula, 0.08f);
        SoftBlob(dl, min + new Vector2(width * (0.95f + 0.05f * Motion.Wave(17900.0)), height * (0.95f + 0.1f * Motion.Wave(12100.0))), height * 1.2f, Styling.AccentBlue, 0.06f);
        dl.PopClipRect();
    }

    private static void SoftBlob(ImDrawListPtr dl, Vector2 center, float radius, Vector4 color, float peak)
    {
        var step = peak * 2f / 18f;
        for (var layer = 18; layer >= 1; layer--)
        {
            var t = layer / 18f;
            var a = step * (1f - Motion.Smoothstep(t)) + step * 0.15f;
            dl.AddCircleFilled(center, radius * t, Paint.Col(Styling.WithAlpha(color, a)), 64);
        }
    }

    private static void DrawHeroIcon(ImDrawListPtr dl, Vector2 center, float size)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var radius = size * 0.5f;
        ProgressRing.Glow(center, radius * 1.05f, Styling.AccentGlow, 0.5f + 0.45f * Styling.Pulse(2600.0));
        ProgressRing.Sweep(center, radius * 1.28f, 2f * globalScale, Styling.AccentGlowSoft, 3400.0, MathF.PI / 2f, 0.9f);
        OrbitParticles(dl, center, radius * 1.28f, 3, 4600.0, 1, Styling.AccentGlowSoft, 2.2f * globalScale);
        var min = center - new Vector2(radius, radius);
        var max = center + new Vector2(radius, radius);
        var rounding = size * 0.22f;
        AppIcon.Draw(dl, min, max, rounding);
        Paint.Stroke(dl, min, max, Styling.WithAlpha(Styling.AccentGlowSoft, 0.55f), rounding, 1.5f * globalScale);
        IconEasterEgg(min, max, globalScale);
    }

    private static void OrbitParticles(ImDrawListPtr dl, Vector2 center, float radius, int count, double periodMs, int direction, Vector4 color, float dotRadius)
    {
        var startAngle = -MathF.PI / 2f + direction * Styling.Phase(periodMs) * MathF.PI * 2f;
        for (var i = 0; i < count; i++)
        {
            var angle = startAngle + i * (MathF.PI * 2f / count);
            var dotCenter = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            dl.AddCircleFilled(dotCenter, dotRadius * 2.4f, Paint.Col(Styling.WithAlpha(color, 0.16f)));
            dl.AddCircleFilled(dotCenter, dotRadius, Paint.Col(color));
        }
    }

    private static float HeroTextWidth()
    {
        float width;
        using (Fonts.PushTitle())
        {
            width = TextDraw.Measure(Name).X;
        }
        width = MathF.Max(width, TextDraw.Measure(Punchline).X);
        using (Fonts.PushCaption())
        {
            return MathF.Max(width, ChipWidth(FontAwesomeIcon.Tag, Loc.T(L.About.Version, version)));
        }
    }

    private static void DrawHeroText(float x, float width, float top, float height)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var punchline = Punchline;
        float titleHeight;
        using (Fonts.PushTitle())
        {
            titleHeight = TextDraw.LineHeight();
        }
        var punchlineHeight = (punchline.Length > 0) ? (TextDraw.LineHeight() + HeroLineGap * globalScale) : 0f;
        float chipHeight;
        using (Fonts.PushCaption())
        {
            chipHeight = TextDraw.LineHeight() + 8f * globalScale;
        }
        var totalHeight = titleHeight + HeroLineGap * globalScale + punchlineHeight + HeroLineGap * globalScale + chipHeight;
        var y = top + (height - totalHeight) * 0.5f;
        DrawShimmerTitle(Name, new Vector2(x, y), width);
        y += titleHeight + HeroLineGap * globalScale;
        if (punchline.Length > 0)
        {
            TextDraw.At(TextDraw.Truncate(punchline, width), new Vector2(x, y), Styling.TextSecondary);
            y += punchlineHeight;
        }
        y += HeroLineGap * globalScale;
        using (Fonts.PushCaption())
        {
            StaticChip(FontAwesomeIcon.Tag, Loc.T(L.About.Version, version), Styling.AccentGlow, new Vector2(x, y), chipHeight);
        }
    }

    private static void DrawShimmerTitle(string text, Vector2 position, float width)
    {
        using (Fonts.PushTitle())
        {
            var textSize = TextDraw.Measure(text);
            var windowDrawList = ImGui.GetWindowDrawList();
            windowDrawList.PushClipRect(position, position + new Vector2(width, textSize.Y), intersectWithCurrentClipRect: true);
            var bloomColor = Styling.WithAlpha(Styling.AccentGlow, 0.22f);
            for (var i = 0; i < BloomOffsets.Length; i++)
            {
                TextDraw.At(text, position + BloomOffsets[i] * ImGuiHelpers.GlobalScale, bloomColor);
            }
            TextDraw.At(text, position, Styling.TextStrong);
            var bandWidth = textSize.X * 0.4f;
            var bandCenter = position.X - bandWidth + Styling.Phase(3400.0) * (textSize.X + bandWidth * 2f);
            windowDrawList.PushClipRect(new Vector2(bandCenter - bandWidth * 0.5f, position.Y), new Vector2(bandCenter + bandWidth * 0.5f, position.Y + textSize.Y), intersectWithCurrentClipRect: true);
            TextDraw.At(text, position, Styling.AccentGlowSoft);
            windowDrawList.PopClipRect();
            windowDrawList.PopClipRect();
        }
    }

    private static float ChipWidth(FontAwesomeIcon icon, string label)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        return 20f * globalScale + TextDraw.IconSize(icon).X + ChipIconGap * globalScale + TextDraw.Measure(label).X;
    }

    private static float StaticChip(FontAwesomeIcon icon, string label, Vector4 accent, Vector2 origin, float height)
    {
        var width = ChipWidth(icon, label);
        var max = origin + new Vector2(width, height);
        Paint.Pill(ImGui.GetWindowDrawList(), origin, max, Styling.WithAlpha(accent, 0.12f), Styling.WithAlpha(accent, 0.38f));
        DrawChipContent(icon, label, accent, Styling.TextSecondary, origin, height);
        return width;
    }

    private static void DrawChipContent(FontAwesomeIcon icon, string label, Vector4 iconColor, Vector4 textColor, Vector2 origin, float height)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var centerY = origin.Y + height * 0.5f;
        var iconSize = TextDraw.IconSize(icon);
        var x = origin.X + ChipPadX * globalScale;
        TextDraw.Icon(icon, new Vector2(x, centerY - iconSize.Y * 0.5f), iconColor);
        x += iconSize.X + ChipIconGap * globalScale;
        TextDraw.At(label, new Vector2(x, centerY - TextDraw.LineHeight() * 0.5f), textColor);
    }

    private void DrawFooter()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        using (Fonts.PushCaption())
        {
            var madeBy = Loc.T(L.About.MadeBy, Author);
            var iconSize = TextDraw.IconSize(FontAwesomeIcon.Code);
            var textSize = TextDraw.Measure(madeBy);
            var gap = FooterGap * globalScale;
            var x = cursorScreenPos.X + (columnWidth - iconSize.X - gap - textSize.X) * 0.5f;
            var pulse = Styling.Pulse(2600.0);
            TextDraw.Icon(FontAwesomeIcon.Code, new Vector2(x, cursorScreenPos.Y + (textSize.Y - iconSize.Y) * 0.5f), Vector4.Lerp(Styling.AccentBlue, Styling.Lighten(Styling.AccentBlueSoft, 0.3f), pulse));
            TextDraw.At(madeBy, new Vector2(x + iconSize.X + gap, cursorScreenPos.Y), Styling.TextDim);
            var lineY = cursorScreenPos.Y + textSize.Y + gap;
            foreach (var line in CreditLines)
            {
                var lineSize = TextDraw.Measure(line);
                TextDraw.At(line, new Vector2(cursorScreenPos.X + (columnWidth - lineSize.X) * 0.5f, lineY), Styling.TextDim);
                lineY += lineSize.Y;
            }
            ImGui.Dummy(new Vector2(columnWidth, lineY - cursorScreenPos.Y));
        }
    }

    private static void SectionHeader(FontAwesomeIcon icon, string label, Vector4 accent, float width)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var iconSize = TextDraw.IconSize(icon);
        var labelSize = TextDraw.SmallCapsSize(label);
        var centerY = cursorScreenPos.Y + iconSize.Y * 0.5f;
        TextDraw.Icon(icon, cursorScreenPos, accent);
        var labelX = cursorScreenPos.X + iconSize.X + 8f * globalScale;
        TextDraw.SmallCaps(label, new Vector2(labelX, centerY - labelSize.Y * 0.5f), Styling.TextDim);
        var ruleX = labelX + labelSize.X + 12f * globalScale;
        Paint.GradientH(ImGui.GetWindowDrawList(), new Vector2(ruleX, centerY), new Vector2(cursorScreenPos.X + width, centerY + 1f), Styling.WithAlpha(accent, 0.5f), Styling.WithAlpha(accent, 0f), 0f);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(width, iconSize.Y));
        Styling.VSpace(4f);
    }

    private static bool OpenOrCopy(Hit.Result hit, string url)
    {
        if (!hit.Hovered)
        {
            return false;
        }
        Tooltip.Show(Loc.T(L.About.LinkHint));
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.SetClipboardText(url);
        }
        if (!hit.Clicked)
        {
            return false;
        }
        OpenUrl(url);
        return true;
    }

    private static void OpenUrl(string url)
    {
        UrlActions.OpenInBrowser(url, (Exception exception) =>
        {
            RunLog.Warning(exception, "failed to launch browser for " + url + ", copied to clipboard instead");
        });
    }

    private static void IconEasterEgg(Vector2 min, Vector2 max, float scale)
    {
        if (!Hit.HoveringRect(min, max))
        {
            iconHovered = false;
            return;
        }
        if (!iconHovered)
        {
            iconHovered = true;
            factCategory = (factCategory + 1) % Categories.Length;
            factLine = NextLineInCategory(factCategory);
        }
        var category = Categories[Math.Max(0, factCategory)];
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        using (Tooltip.Begin())
        {
            using (Fonts.PushIcon())
            {
                using (ImRaii.PushColor(ImGuiCol.Text, category.Color))
                {
                    ImGui.TextUnformatted(category.Icon.ToIconString());
                }
            }
            ImGui.SameLine(0f, 8f * scale);
            using (ImRaii.PushColor(ImGuiCol.Text, category.Color))
            {
                ImGui.TextUnformatted(Loc.T(category.Header));
            }
            ImGui.Spacing();
            Tooltip.Text(Loc.T(category.Lines[factLine]));
        }
    }

    private static int NextLineInCategory(int category)
    {
        var lineCount = Categories[category].Lines.Length;
        if (factBags[category] == null || factBagPositions[category] >= lineCount)
        {
            var avoidFirst = (factBags[category] == null) ? (-1) : factLastServed[category];
            factBags[category] = Shuffle(lineCount, avoidFirst);
            factBagPositions[category] = 0;
        }
        var line = factBags[category][factBagPositions[category]++];
        factLastServed[category] = line;
        return line;
    }

    private static int[] Shuffle(int count, int avoidFirst)
    {
        var order = new int[count];
        for (var i = 0; i < count; i++)
        {
            order[i] = i;
        }
        for (var i = count - 1; i > 0; i--)
        {
            var swapIndex = Random.Shared.Next(i + 1);
            (order[i], order[swapIndex]) = (order[swapIndex], order[i]);
        }
        if (count > 1 && order[0] == avoidFirst)
        {
            var swapIndex = 1 + Random.Shared.Next(count - 1);
            (order[0], order[swapIndex]) = (order[swapIndex], order[0]);
        }
        return order;
    }
}
