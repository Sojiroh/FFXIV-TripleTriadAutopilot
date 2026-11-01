using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Tasks;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Sections;

internal static class PlanCard
{
    private enum PieceKind
    {
        Word,
        Subject,
        After
    }

    private readonly record struct Piece(PieceKind Kind, string Text);

    private ref struct Popover
    {
        private ImRaii.StyleDisposable style;

        private ImRaii.PopupDisposable popup;

        public bool Open => popup.Alive;

        public Popover(string id, Vector2 anchor, float width, long openedTick)
        {
            var globalScale = ImGuiHelpers.GlobalScale;
            var reveal = Motion.Reveal(openedTick, PopoverRevealMs);
            ImGui.SetNextWindowPos(PopoverPosition(anchor, width) - new Vector2(0f, (1f - reveal) * PopoverSlide * globalScale), ImGuiCond.Always);
            style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, PopoverPadding * globalScale).Push(ImGuiStyleVar.Alpha, MathF.Max(0.05f, reveal));
            popup = ImRaii.Popup(id, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize);
        }

        public void Dispose()
        {
            popup.Dispose();
            style.Dispose();
        }
    }

    private const float PadX = 18f;

    private const float PadY = 16f;

    private const float TokenHeight = 30f;

    private const float TokenPadX = 11f;

    private const float ChevronGap = 6f;

    private const float WordGap = 8f;

    private const float LineGap = 10f;

    private const float PopoverWidth = 420f;

    private const float PopoverGap = 6f;

    private const float PopoverRevealMs = 220f;

    private const float PopoverSlide = 8f;

    private const int QueuePreviewCount = 4;

    private const string AfterPopup = "##tta_after_popover";

    private const string AfterTokenId = "##tta_token_after";

    private static readonly string[] subjectTokenIds = new string[2] { "##tta_token_cards", "##tta_token_npcs" };

    private static readonly AfterRunAction[] afterRunOrder = new AfterRunAction[4]
    {
        AfterRunAction.StayLoggedIn,
        AfterRunAction.ReturnToInn,
        AfterRunAction.Logout,
        AfterRunAction.CloseGame
    };

    private static readonly (LocString Token, LocString Name, LocString Detail)[] afterRunChoices = new (LocString, LocString, LocString)[4]
    {
        (L.Triad.AfterStayToken, L.Triad.AfterStayName, L.Triad.AfterStayDetail),
        (L.Triad.AfterInnToken, L.Triad.AfterInnName, L.Triad.AfterInnDetail),
        (L.Triad.AfterLogoutToken, L.Triad.AfterLogoutName, L.Triad.AfterLogoutDetail),
        (L.Triad.AfterCloseToken, L.Triad.AfterCloseName, L.Triad.AfterCloseDetail)
    };

    private static readonly Piece[] pieces = new Piece[5];

    private static readonly Vector2 PopoverPadding = new Vector2(16f, 16f);

    private static Vector2 afterAnchor;

    private static long afterOpenedTick;

    private static CachedText queueText;

    private static CachedText unavailableText;

    public static bool Draw(Configuration configuration, AutoTriadController controller)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var padX = PadX * globalScale;
        var padY = PadY * globalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var assessment = TriadLauncher.Assess(configuration);
        windowDrawList.ChannelsSplit(2);
        windowDrawList.ChannelsSetCurrent(1);
        var cursorY = cursorScreenPos.Y + padY;
        var label = Loc.T(L.Triad.Plan);
        var titleSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, new Vector2(cursorScreenPos.X + padX, cursorY), Styling.TextStrong);
        cursorY += titleSize.Y + 10f * globalScale;
        var subjectClicked = DrawSentence(configuration, controller, in assessment, new Vector2(cursorScreenPos.X + padX, cursorY), availableWidth - padX * 2f, out var bottom);
        cursorY = bottom + 14f * globalScale;
        ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + padX, cursorY));
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 6f) * globalScale))
        {
            ImGui.PushID("##tta_plan_chips");
            ImGui.BeginGroup();
            var color = ((assessment.Readiness == TriadLauncher.Readiness.Ready) ? Styling.TextDim : Styling.TextMuted);
            DrawCaption(QueueCaption(configuration, in assessment), color, availableWidth - padX * 2f);
            if (assessment.Plan.Unavailable.Length != 0)
            {
                DrawCaption(UnavailableCaption(in assessment), Styling.AccentAmber, availableWidth - padX * 2f);
            }
            ImGui.EndGroup();
            ImGui.PopID();
        }
        var max = new Vector2(cursorScreenPos.X + availableWidth, ImGui.GetItemRectMax().Y + padY);
        windowDrawList.ChannelsSetCurrent(0);
        Paint.Glass(windowDrawList, cursorScreenPos, max, Styling.PanelRounding * globalScale, Styling.AccentGlow, 0.07f, 0f, elevated: true);
        windowDrawList.ChannelsMerge();
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(availableWidth, max.Y - cursorScreenPos.Y));
        DrawAfterPopover(configuration);
        return subjectClicked;
    }

    private static bool DrawSentence(Configuration configuration, AutoTriadController controller, in TriadLauncher.Assessment assessment, Vector2 start, float maxWidth, out float bottom)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var tokenHeight = TokenHeight * globalScale;
        var wordGap = WordGap * globalScale;
        var mode = assessment.Mode;
        var sentenceEnd = Loc.T(L.Triad.SentenceEnd);
        var pieceCount = 0;
        pieces[pieceCount++] = new Piece(PieceKind.Word, Loc.T((mode == TriadRunMode.Farm) ? L.Triad.SentenceFarm : L.Triad.SentenceCollect));
        pieces[pieceCount++] = new Piece(PieceKind.Subject, SubjectToken(in assessment));
        pieces[pieceCount++] = new Piece(PieceKind.Word, Loc.T(L.Triad.SentenceThen));
        pieces[pieceCount++] = new Piece(PieceKind.After, Loc.T(afterRunChoices[AfterIndex(configuration)].Token));
        if (sentenceEnd.Length > 0)
        {
            pieces[pieceCount++] = new Piece(PieceKind.Word, sentenceEnd);
        }
        var cursorX = start.X;
        var cursorY = start.Y;
        var subjectClicked = false;
        var enabled = !controller.Running;
        var nothingPicked = assessment.Readiness == TriadLauncher.Readiness.NothingPicked;
        for (var i = 0; i < pieceCount; i++)
        {
            var piece = pieces[i];
            var isSentenceEnd = (object)piece.Text == sentenceEnd;
            var pieceWidth = ((piece.Kind == PieceKind.Word) ? TextDraw.Measure(piece.Text).X : TokenWidth(piece.Text));
            var trailingGap = (isSentenceEnd ? 0f : wordGap);
            if (cursorX > start.X && cursorX + pieceWidth > start.X + maxWidth)
            {
                cursorX = start.X;
                cursorY += tokenHeight + LineGap * globalScale;
            }
            if (piece.Kind == PieceKind.Word)
            {
                var wordSize = TextDraw.Measure(piece.Text);
                TextDraw.At(piece.Text, new Vector2(cursorX, cursorY + (tokenHeight - wordSize.Y) * 0.5f), Styling.TextSecondary);
                cursorX += pieceWidth + trailingGap;
                continue;
            }
            var accent = (((piece.Kind == PieceKind.Subject) & nothingPicked) ? Styling.AccentAmber : Styling.AccentGlow);
            var tokenAnchor = new Vector2(cursorX, cursorY + tokenHeight + PopoverGap * globalScale);
            ImGui.SetCursorScreenPos(new Vector2(cursorX, cursorY));
            var tokenClicked = DrawToken(TokenId(piece.Kind, mode), piece.Text, accent, enabled);
            if (piece.Kind == PieceKind.Subject)
            {
                subjectClicked |= tokenClicked;
            }
            else
            {
                afterAnchor = tokenAnchor;
                if (tokenClicked)
                {
                    afterOpenedTick = OpenPopover(AfterPopup);
                }
            }
            cursorX += pieceWidth + trailingGap;
        }
        bottom = cursorY + tokenHeight;
        return subjectClicked;
    }

    private static string SubjectToken(in TriadLauncher.Assessment assessment)
    {
        if (assessment.Mode == TriadRunMode.Farm)
        {
            if (assessment.Npcs != 0)
            {
                return Loc.Plural(L.Triad.NpcsCount, assessment.Npcs);
            }
            return Loc.T(L.Triad.NpcsNone);
        }
        if (assessment.Cards != 0)
        {
            return Loc.Plural(L.Triad.CardsCount, assessment.Cards);
        }
        return Loc.T(L.Triad.CardsNone);
    }

    private static string TokenId(PieceKind kind, TriadRunMode mode)
    {
        if (kind != PieceKind.Subject)
        {
            return AfterTokenId;
        }
        return subjectTokenIds[Math.Clamp((int)mode, 0, subjectTokenIds.Length - 1)];
    }

    private static string QueueCaption(Configuration configuration, in TriadLauncher.Assessment assessment)
    {
        if (assessment.Readiness != TriadLauncher.Readiness.Ready)
        {
            return TriadLauncher.Reason(in assessment);
        }
        if (assessment.Mode == TriadRunMode.Farm)
        {
            if (configuration.FarmStop != FarmStopKind.Matches)
            {
                return Loc.T(L.Triad.FarmCaptionNever);
            }
            return Loc.Plural(L.Triad.FarmCaptionMatches, configuration.FarmMatchLimit);
        }
        var assignments = assessment.Plan.Assignments;
        var key = ((long)TriadLauncher.SelectionVersion << 20) | (uint)assignments.Length;
        if (queueText.TryGet(key, out string cached))
        {
            return cached;
        }
        var set = TriadData.Set;
        var names = string.Empty;
        var shown = Math.Min(QueuePreviewCount, assignments.Length);
        for (var i = 0; i < shown; i++)
        {
            var npcName = set.NpcNames[assignments[i].NpcIndex];
            names = ((i == 0) ? npcName : (names + "  ·  " + npcName));
        }
        if (assignments.Length > shown)
        {
            names = names + "  " + Loc.Plural(L.Triad.QueueMore, assignments.Length - shown);
        }
        return queueText.Set(key, Loc.T(L.Triad.QueueCaption, names));
    }

    private static string UnavailableCaption(in TriadLauncher.Assessment assessment)
    {
        var unavailable = assessment.Plan.Unavailable;
        var key = ((long)TriadLauncher.SelectionVersion << 20) | (uint)unavailable.Length;
        return unavailableText.Get(key, (long packed) => Loc.Plural(L.Triad.UnavailableCaption, (int)(packed & 0xFFFFF)));
    }

    private static void DrawCaption(string text, Vector4 color, float maxWidth)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(text, maxWidth - 2f * globalScale), new Vector2(cursorScreenPos.X + 2f * globalScale, cursorScreenPos.Y), color);
            ImGui.Dummy(new Vector2(maxWidth, ImGui.GetTextLineHeight()));
        }
    }

    private static float TokenWidth(string label)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        return TokenPadX * 2f * globalScale + TextDraw.Measure(label).X + ChevronGap * globalScale + TextDraw.IconSize(FontAwesomeIcon.ChevronDown).X;
    }

    private static bool DrawToken(string id, string label, Vector4 accent, bool enabled)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var tokenSize = new Vector2(TokenWidth(label), TokenHeight * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + tokenSize;
        var hit = Hit.Area(id, tokenSize, enabled);
        var hover = Motion.Hover(Motion.Key(id), hit.Hovered);
        var windowDrawList = ImGui.GetWindowDrawList();
        var fill = (enabled ? Styling.WithAlpha(accent, 0.16f + 0.12f * hover) : Styling.WithAlpha(Styling.Surface2, 0.8f));
        var border = (enabled ? Styling.WithAlpha(accent, 0.45f + 0.3f * hover) : Styling.WithAlpha(Styling.BorderDim, 0.6f));
        var textColor = (enabled ? Vector4.Lerp(Styling.Lighten(accent, 0.3f), Styling.TextStrong, hover * 0.5f) : Styling.TextDim);
        if (hit.Held)
        {
            fill = Styling.Darken(fill, 0.12f);
        }
        Paint.Pill(windowDrawList, cursorScreenPos, max, fill, border);
        var centerY = cursorScreenPos.Y + tokenSize.Y * 0.5f;
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(cursorScreenPos.X + TokenPadX * globalScale, centerY - labelSize.Y * 0.5f), textColor);
        var chevronSize = TextDraw.IconSize(FontAwesomeIcon.ChevronDown);
        TextDraw.Icon(FontAwesomeIcon.ChevronDown, new Vector2(max.X - TokenPadX * globalScale - chevronSize.X, centerY - chevronSize.Y * 0.5f + 1f * globalScale), Styling.WithAlpha(textColor, 0.75f));
        if (!enabled && Hit.HoveringRect(cursorScreenPos, max))
        {
            Tooltip.Show(Loc.T(L.Triad.PlanLocked));
        }
        return hit.Clicked;
    }

    private static int AfterIndex(Configuration configuration)
    {
        return Math.Max(0, Array.IndexOf(afterRunOrder, configuration.AfterRun));
    }

    private static Vector2 PopoverPosition(Vector2 anchor, float contentWidth)
    {
        var mainViewport = ImGui.GetMainViewport();
        var popoverWidth = contentWidth + PopoverPadding.X * 2f * ImGuiHelpers.GlobalScale;
        var maxX = mainViewport.WorkPos.X + mainViewport.WorkSize.X - popoverWidth;
        var position = anchor;
        position.X = MathF.Max(mainViewport.WorkPos.X, MathF.Min(anchor.X, maxX));
        return position;
    }

    private static long OpenPopover(string id)
    {
        ImGui.OpenPopup(id);
        return Environment.TickCount64;
    }

    private static void DrawAfterPopover(Configuration configuration)
    {
        if (!ImGui.IsPopupOpen(AfterPopup))
        {
            return;
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var width = PopoverWidth * globalScale;
        using var popover = new Popover(AfterPopup, afterAnchor, width, afterOpenedTick);
        if (!popover.Open)
        {
            return;
        }
        var selectedIndex = AfterIndex(configuration);
        DrawPopoverHeading(Loc.T(L.Triad.WhenDone), width, 8f);
        for (var i = 0; i < afterRunChoices.Length; i++)
        {
            if (DrawChoiceRow(i, Loc.T(afterRunChoices[i].Name), Loc.T(afterRunChoices[i].Detail), i == selectedIndex, width))
            {
                configuration.AfterRun = afterRunOrder[i];
                configuration.SaveDebounced();
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private static void DrawPopoverHeading(string heading, float width, float gapBelow)
    {
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var headingSize = TextDraw.SectionTitleSize(heading);
        TextDraw.SectionTitle(heading, cursorScreenPos, Styling.TextStrong);
        ImGui.Dummy(new Vector2(width, headingSize.Y + gapBelow * ImGuiHelpers.GlobalScale));
    }

    private static bool DrawChoiceRow(int index, string name, string detail, bool selected, float width)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var padX = 10f * globalScale;
        var padY = 8f * globalScale;
        var textLineHeight = ImGui.GetTextLineHeight();
        float detailHeight;
        using (Fonts.PushCaption())
        {
            detailHeight = TextDraw.Measure(detail).Y;
        }
        var rowSize = new Vector2(width, padY * 2f + textLineHeight + 2f * globalScale + detailHeight);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        ImGui.PushID(index + 1);
        var hit = Hit.Area("##choice", rowSize);
        var hover = Motion.Hover(Motion.Key("##choice"), hit.Hovered);
        ImGui.PopID();
        var windowDrawList = ImGui.GetWindowDrawList();
        var color = (selected ? Styling.WithAlpha(Styling.AccentGlow, 0.18f + 0.08f * hover) : Styling.WithAlpha(Styling.Surface2, 0.8f * hover));
        if (color.W > 0.01f)
        {
            Paint.Fill(windowDrawList, cursorScreenPos, cursorScreenPos + rowSize, color, 8f * globalScale);
        }
        var nameColor = (selected ? Styling.AccentGlowSoft : Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, hover));
        TextDraw.At(name, new Vector2(cursorScreenPos.X + padX, cursorScreenPos.Y + padY), nameColor);
        using (Fonts.PushCaption())
        {
            TextDraw.At(detail, new Vector2(cursorScreenPos.X + padX, cursorScreenPos.Y + padY + textLineHeight + 2f * globalScale), Styling.TextMuted);
        }
        if (selected)
        {
            var checkSize = TextDraw.IconSize(FontAwesomeIcon.Check);
            TextDraw.Icon(FontAwesomeIcon.Check, new Vector2(cursorScreenPos.X + rowSize.X - padX - checkSize.X, cursorScreenPos.Y + padY + (textLineHeight - checkSize.Y) * 0.5f), Styling.AccentGlowSoft);
        }
        return hit.Clicked;
    }
}
