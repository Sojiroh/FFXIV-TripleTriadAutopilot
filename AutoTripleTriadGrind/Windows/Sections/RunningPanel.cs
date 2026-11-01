using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Tasks;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Sections;

internal static class RunningPanel
{
    private const float PadX = 18f;

    private const float HeroCardSize = 34f;

    private const float QueueCardSize = 30f;

    private const float CardGap = 4f;

    private const int QueueLength = 6;

    private static readonly CachedText[] rowMeta = new CachedText[QueueLength];

    private static CachedText footerText;

    private static CachedText matchesText;

    private static CachedText recordText;

    private static CachedText goalText;

    public static void Draw(AutoTriadController controller)
    {
        TriadOwnership.Refresh();
        var paused = controller.Paused;
        (Vector4 Accent, Vector4 AccentSoft, string Label) palette = PhasePalette(controller);
        var accent = palette.Accent;
        var accentSoft = palette.AccentSoft;
        var phaseLabel = palette.Label;
        var workload = RunWorkload.Measure(controller);
        DrawHeaderStrip(Footer(controller), accent, accentSoft, paused);
        Styling.VSpace(6f);
        DrawHeroCard(controller, workload, accent, accentSoft, phaseLabel);
        Styling.VSpace(10f);
        DrawStatTiles(controller);
        Styling.VSpace(10f);
        DrawQueue(controller);
        DrawSkipped(controller);
    }

    private static string Footer(AutoTriadController controller)
    {
        var progress = controller.Progress;
        var key = ((long)progress.QueueNext << 20) | (uint)progress.Queue.Count;
        return footerText.Get(key, (long packed) => Loc.T(L.Run.Footer, Math.Min((int)(packed >> 20) + 1, (int)(packed & 0xFFFFF)), (int)(packed & 0xFFFFF)));
    }

    private static void DrawHeaderStrip(string footer, Vector4 accent, Vector4 accentSoft, bool paused)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var textLineHeight = ImGui.GetTextLineHeight();
        var centerY = cursorScreenPos.Y + textLineHeight * 0.5f;
        var color = (paused ? accent : Styling.PulseColor(accent, accentSoft));
        var dotRadius = 4f * globalScale;
        Paint.Dot(windowDrawList, new Vector2(cursorScreenPos.X + dotRadius + 3f * globalScale, centerY), dotRadius, color);
        var label = (paused ? Loc.T(L.Shell.StatusPaused) : Loc.T(L.Shell.StatusRunning));
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(cursorScreenPos.X + dotRadius * 2f + 12f * globalScale, centerY - labelSize.Y * 0.5f), Styling.TextSecondary);
        using (Fonts.PushCaption())
        {
            var footerSize = TextDraw.Measure(footer);
            TextDraw.At(footer, new Vector2(cursorScreenPos.X + availableWidth - footerSize.X, centerY - footerSize.Y * 0.5f), Styling.TextMuted);
        }
        ImGui.Dummy(new Vector2(availableWidth, textLineHeight));
    }

    private static void DrawHeroCard(AutoTriadController controller, RunWorkload.Workload workload, Vector4 accent, Vector4 accentSoft, string label)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var heroSize = new Vector2(ImGui.GetContentRegionAvail().X, HeroHeight(controller));
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + heroSize;
        var windowDrawList = ImGui.GetWindowDrawList();
        var active = controller.Running && !controller.Paused;
        var rounding = Styling.PanelRounding * globalScale;
        Paint.Glass(windowDrawList, cursorScreenPos, max, rounding, accent, active ? 0.1f : 0.03f, 0f, elevated: true);
        if (active)
        {
            Paint.Stroke(windowDrawList, cursorScreenPos, max, Styling.PulseColor(Styling.WithAlpha(accent, 0.5f), accentSoft), rounding, 1.6f);
        }
        var padding = PadX * globalScale;
        var ringRadius = heroSize.Y * 0.5f - 18f * globalScale;
        var center = new Vector2(cursorScreenPos.X + padding + ringRadius, cursorScreenPos.Y + heroSize.Y * 0.5f);
        DrawRing(center, ringRadius, accent, active, workload);
        var contentLeft = center.X + ringRadius + 20f * globalScale;
        var contentWidth = max.X - padding - contentLeft;
        var cursorY = cursorScreenPos.Y + 16f * globalScale;
        cursorY += DrawPhaseChip(contentLeft, cursorY, label, accent, accentSoft) + 10f * globalScale;
        var currentNpcIndex = controller.Progress.CurrentNpcIndex;
        var set = TriadData.Set;
        var nameLineHeight = ImGui.GetTextLineHeight();
        if (currentNpcIndex != ushort.MaxValue && currentNpcIndex < set.NpcCount)
        {
            var progress = controller.Progress;
            var record = recordText.Get(((long)progress.NpcMatchesWon << 32) | ((long)progress.NpcMatchesLost << 16) | (uint)progress.NpcMatchesDrawn, (long key) => Loc.T(L.Run.Record, (int)(key >> 32), (int)((key >> 16) & 0xFFFF), (int)(key & 0xFFFF)));
            var recordWidth = TextDraw.Measure(record).X;
            TextDraw.At(record, new Vector2(contentLeft + contentWidth - recordWidth, cursorY), accentSoft);
            TextDraw.At(TextDraw.Truncate(set.NpcNames[currentNpcIndex], contentWidth - recordWidth - 12f * globalScale), new Vector2(contentLeft, cursorY), Styling.TextStrong);
            cursorY += nameLineHeight + 3f * globalScale;
            using (Fonts.PushCaption())
            {
                var location = TextDraw.Truncate(TriadLabels.NpcLocation(currentNpcIndex), contentWidth);
                TextDraw.At(location, new Vector2(contentLeft, cursorY), Styling.TextDim);
                TextDraw.Trailing(controller.Status, contentLeft + TextDraw.Measure(location).X, contentLeft + contentWidth, cursorY, Styling.TextMuted);
                cursorY += ImGui.GetTextLineHeight();
            }
        }
        else
        {
            TextDraw.At(TextDraw.Truncate(string.IsNullOrWhiteSpace(controller.Status) ? Loc.T(L.Common.Working) : controller.Status, contentWidth), new Vector2(contentLeft, cursorY), Styling.TextSecondary);
            cursorY += nameLineHeight;
        }
        cursorY += 10f * globalScale;
        var barHeight = 8f * globalScale;
        var origin = new Vector2(contentLeft, cursorY);
        if (controller.Phase == TriadPhase.Optimizing)
        {
            Paint.Bar(windowDrawList, origin, contentWidth, barHeight, controller.Progress.OptimizerProgress, accent);
        }
        else if (active)
        {
            Paint.IndeterminateBar(windowDrawList, origin, contentWidth, barHeight, accent);
        }
        else
        {
            Paint.Bar(windowDrawList, origin, contentWidth, barHeight, 0f, accent);
        }
        cursorY += barHeight + 8f * globalScale;
        if (currentNpcIndex != ushort.MaxValue && currentNpcIndex < set.NpcCount)
        {
            DrawCardStrip(windowDrawList, set.RewardsOf(currentNpcIndex), contentLeft, cursorY, HeroCardSize * globalScale, max.X - padding);
        }
        ImGui.Dummy(heroSize);
    }

    private static float HeroHeight(AutoTriadController controller)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var minHeight = Layout.HeroCardHeight * globalScale;
        var currentNpcIndex = controller.Progress.CurrentNpcIndex;
        if (currentNpcIndex == ushort.MaxValue || currentNpcIndex >= TriadData.Set.NpcCount)
        {
            return minHeight;
        }
        float captionLineHeight;
        using (Fonts.PushCaption())
        {
            captionLineHeight = ImGui.GetTextLineHeight();
        }
        var chipHeight = captionLineHeight + 6f * globalScale;
        var y = 16f * globalScale + chipHeight + 10f * globalScale + ImGui.GetTextLineHeight() + 3f * globalScale + captionLineHeight + 10f * globalScale + 8f * globalScale + 8f * globalScale + HeroCardSize * globalScale + 16f * globalScale;
        return MathF.Max(minHeight, y);
    }

    private static void DrawCardStrip(ImDrawListPtr drawList, ReadOnlySpan<ushort> cards, float x, float y, float size, float rightX)
    {
        var gap = CardGap * ImGuiHelpers.GlobalScale;
        for (var i = 0; i < cards.Length; i++)
        {
            var left = x + (float)i * (size + gap);
            if (left + size > rightX)
            {
                return;
            }
            var cardId = cards[i];
            CardIcon.Draw(drawList, cardId, new Vector2(left, y), size, TriadOwnership.IsOwned(cardId) ? CardIcon.State.Owned : CardIcon.State.Missing);
        }
    }

    private static void DrawRing(Vector2 center, float radius, Vector4 accent, bool active, RunWorkload.Workload workload)
    {
        var thickness = 6f * ImGuiHelpers.GlobalScale;
        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        if (workload.Needed == 0)
        {
            if (active)
            {
                ProgressRing.Sweep(center, radius, thickness, accent, Styling.PulseOrbit, 1.8849558f, 1f);
            }
            ProgressRing.CenterIcon(center, FontAwesomeIcon.ThLarge, Styling.TextDim, radius * 0.55f);
            return;
        }
        var fraction = Motion.Approach(Motion.Key("##tta_run_ring"), workload.Fraction, 6f);
        ProgressRing.Fill(center, radius, thickness, fraction, accent);
        var small = goalText.Get(workload.Needed, (long key) => Loc.T(L.Run.GoalOf, (int)key));
        ProgressRing.CenterValue(center, workload.Done.ToString(Loc.Culture), small, Styling.TextStrong, Styling.TextDim);
    }

    private static float DrawPhaseChip(float x, float y, string text, Vector4 accent, Vector4 accentSoft)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var padX = 9f * globalScale;
        var padY = 3f * globalScale;
        using (Fonts.PushCaption())
        {
            var upper = TextDraw.Upper(text);
            var textSize = TextDraw.Measure(upper);
            var chipMin = new Vector2(x, y);
            var max = chipMin + new Vector2(padX * 2f + textSize.X, textSize.Y + padY * 2f);
            Paint.Pill(windowDrawList, chipMin, max, Styling.WithAlpha(accent, 0.28f), Styling.WithAlpha(accent, 0.65f));
            TextDraw.At(upper, new Vector2(x + padX, y + padY), accentSoft);
            return max.Y - chipMin.Y;
        }
    }

    private static (Vector4 Accent, Vector4 AccentSoft, string Label) PhasePalette(AutoTriadController controller)
    {
        if (!controller.Running)
        {
            return (Accent: Styling.TextDim, AccentSoft: Styling.TextSecondary, Label: Loc.T(L.Run.PhaseReady));
        }
        if (controller.Paused)
        {
            if (controller.PauseReason != PauseReason.InContent)
            {
                return (Accent: Styling.AccentAmber, AccentSoft: Styling.AccentAmberSoft, Label: Loc.T(L.Run.PhasePaused));
            }
            return (Accent: Styling.AccentAmber, AccentSoft: Styling.AccentAmberSoft, Label: Loc.T(L.Run.PhasePausedInContent));
        }
        return controller.Phase switch
        {
            TriadPhase.Playing => (Accent: Styling.AccentGlow, AccentSoft: Styling.AccentGlowSoft, Label: ReadyState.PhaseLabel(controller.Phase)),
            TriadPhase.Finishing => (Accent: Styling.AccentMint, AccentSoft: Styling.AccentMintSoft, Label: ReadyState.PhaseLabel(controller.Phase)),
            TriadPhase.Idle => (Accent: Styling.TextDim, AccentSoft: Styling.TextSecondary, Label: ReadyState.PhaseLabel(controller.Phase)),
            _ => (Accent: Styling.AccentBlue, AccentSoft: Styling.AccentBlueSoft, Label: ReadyState.PhaseLabel(controller.Phase)),
        };
    }

    private static void DrawStatTiles(AutoTriadController controller)
    {
        var sessionSnapshot = controller.SessionSnapshot;
        var globalScale = ImGuiHelpers.GlobalScale;
        var gap = 8f * globalScale;
        var width = (ImGui.GetContentRegionAvail().X - gap * 3f) / 4f;
        var won = sessionSnapshot?.MatchesWon ?? 0;
        var lost = sessionSnapshot?.MatchesLost ?? 0;
        var drawn = sessionSnapshot?.MatchesDrawn ?? 0;
        var sub = matchesText.Get(((long)won << 32) | ((long)lost << 16) | (uint)drawn, (long key) => Loc.T(L.Run.Record, (int)(key >> 32), (int)((key >> 16) & 0xFFFF), (int)(key & 0xFFFF)));
        StatTile.Draw(Loc.T(L.Run.TileMatches), (won + lost + drawn).ToString(Loc.Culture), sub, Styling.AccentGlow, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Run.TileCards), (sessionSnapshot?.CardsObtained.Count ?? 0).ToString(Loc.Culture), null, Styling.AccentMint, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Run.TileNpcs), (sessionSnapshot?.NpcsCompleted ?? 0).ToString(Loc.Culture), null, Styling.AccentAmber, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Run.TileElapsed), Formatting.Elapsed(sessionSnapshot?.Elapsed ?? TimeSpan.Zero), null, Styling.AccentBlue, width);
    }

    private static void DrawQueue(AutoTriadController controller)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var label = Loc.T(L.Run.UpNext);
        var titleSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, cursorScreenPos, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, titleSize.Y + 8f * globalScale));
        var progress = controller.Progress;
        var queue = progress.Queue;
        var next = Math.Min(progress.QueueNext, queue.Count);
        if (next >= queue.Count)
        {
            TextDraw.Hint(Loc.T((queue.Count == 0) ? L.Run.QueuePlanning : L.Run.QueueDone));
            return;
        }
        var visible = Math.Min(QueueLength, queue.Count - next);
        for (var i = 0; i < visible; i++)
        {
            DrawQueueRow(i, queue[next + i], i == 0);
        }
    }

    private static void DrawQueueRow(int slot, ushort npcIndex, bool emphasize)
    {
        var set = TriadData.Set;
        var globalScale = ImGuiHelpers.GlobalScale;
        var rowSize = new Vector2(ImGui.GetContentRegionAvail().X, Layout.QueueRowHeight * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + rowSize;
        var windowDrawList = ImGui.GetWindowDrawList();
        Paint.Glass(windowDrawList, cursorScreenPos, max, Styling.CardRounding * globalScale, Styling.AccentGlow, emphasize ? 0.1f : 0.03f);
        var padding = 13f * globalScale;
        var centerY = cursorScreenPos.Y + rowSize.Y * 0.5f;
        var cards = set.RewardsOf(npcIndex);
        var cardSize = QueueCardSize * globalScale;
        var cardsWidth = (float)cards.Length * (cardSize + CardGap * globalScale);
        var cardsLeft = max.X - padding - cardsWidth;
        DrawCardStrip(windowDrawList, cards, cardsLeft, centerY - cardSize * 0.5f, cardSize, max.X);
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.User);
        var textLineHeight = ImGui.GetTextLineHeight();
        TextDraw.Icon(FontAwesomeIcon.User, new Vector2(cursorScreenPos.X + padding, centerY - textLineHeight + (textLineHeight - iconSize.Y) * 0.5f), emphasize ? Styling.AccentGlow : Styling.TextDim);
        var textLeft = cursorScreenPos.X + padding + iconSize.X + 10f * globalScale;
        var maxWidth = cardsLeft - 12f * globalScale - textLeft;
        TextDraw.At(TextDraw.Truncate(set.NpcNames[npcIndex], maxWidth), new Vector2(textLeft, centerY - textLineHeight), emphasize ? Styling.TextStrong : Styling.TextSecondary);
        var text = rowMeta[slot].Get(npcIndex, (long key) => TriadLabels.NpcLocation((int)key));
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(text, maxWidth), new Vector2(textLeft, centerY + 2f * globalScale), Styling.TextDim);
        }
        ImGui.Dummy(rowSize);
    }

    private static void DrawSkipped(AutoTriadController controller)
    {
        var sessionSnapshot = controller.SessionSnapshot;
        if (sessionSnapshot == null || sessionSnapshot.SkippedNpcs.Count == 0)
        {
            return;
        }
        var set = TriadData.Set;
        GroupLabel.Draw(Loc.T(L.Run.Skipped), 10f, 4f);
        var skippedNpcs = sessionSnapshot.SkippedNpcs;
        for (var i = 0; i < skippedNpcs.Count; i++)
        {
            var skippedNpc = skippedNpcs[i];
            if (skippedNpc.NpcIndex < set.NpcCount)
            {
                using (Fonts.PushCaption())
                {
                    ImGui.TextColored(in Styling.AccentAmber, set.NpcNames[skippedNpc.NpcIndex] + "  ·  " + TriadLabels.Skip(skippedNpc.Reason));
                }
            }
        }
    }
}
