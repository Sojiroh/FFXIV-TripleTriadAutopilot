using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Sections.Config;

internal static class SettingsControls
{
    internal static class Choices
    {
        public readonly record struct Choice(LocString Name, LocString Detail);

        private const float PanelWidth = 320f;

        private static string[] names = Array.Empty<string>();

        private static string[] details = Array.Empty<string>();

        public static void DrawCombo(string id, Choice[] options, int selected, Action<int> onSelect, float width = RowComboWidth)
        {
            Resolve(options);
            var selectedIndex = selected;
            if (Dropdown.DrawDetailed(id, names.AsSpan(0, options.Length), details.AsSpan(0, options.Length), ref selectedIndex, width, PanelWidth))
            {
                onSelect(selectedIndex);
            }
        }

        private static void Resolve(Choice[] options)
        {
            if (names.Length < options.Length)
            {
                names = new string[options.Length];
                details = new string[options.Length];
            }
            for (var i = 0; i < options.Length; i++)
            {
                names[i] = Loc.T(options[i].Name);
                details[i] = Loc.T(options[i].Detail);
            }
        }
    }

    public const float ToggleWidth = 40f;

    public const float RowSliderWidth = 180f;

    public const float RowComboWidth = 170f;

    private const float RangeDragWidth = 62f;

    private const float RangeDashSlot = 14f;

    private const float RangeDragSpeed = 0.25f;

    private static readonly string[] languageLabels = BuildLanguageLabels();

    public static float RangeInlineWidth()
        => RangeDragWidth + RangeDashSlot + RangeDragWidth;

    public static void DrawToggle(Configuration cfg, Func<bool> getter, Action<bool> setter, string id)
    {
        var value = getter();
        if (ToggleSwitch.Draw(id, ref value))
        {
            setter(value);
            cfg.SaveDebounced();
        }
    }

    public static void DrawIntSlider(Configuration cfg, string id, Func<int> getter, Action<int> setter, int minimum, int maximum, string format = "%d", float width = RowSliderWidth)
    {
        var v = getter();
        ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
        using (PushFrameColors())
        {
            if (ImGui.SliderInt(id, ref v, minimum, maximum, format))
            {
                setter(Math.Clamp(v, minimum, maximum));
                cfg.SaveDebounced();
            }
        }
    }

    public static bool DrawPlainCombo(string id, ref int index, string[] labels, float width)
        => Dropdown.Draw(id, labels, ref index, width);

    public static void DrawLanguageCombo(Configuration cfg, float width = RowComboWidth)
    {
        var all = Languages.All;
        var selected = 0;
        for (var i = 0; i < all.Length; i++)
        {
            if (all[i] == Loc.Current)
            {
                selected = i;
            }
        }
        if (Dropdown.Draw("##tta_language", languageLabels, ref selected, width))
        {
            ApplyLanguage(cfg, all[selected].Code);
        }
    }

    private static string[] BuildLanguageLabels()
    {
        var all = Languages.All;
        var labels = new string[all.Length];
        for (var i = 0; i < all.Length; i++)
        {
            labels[i] = all[i].NativeName;
        }
        return labels;
    }

    private static void ApplyLanguage(Configuration cfg, string code)
    {
        cfg.Language = code;
        cfg.Save();
        Loc.SetLanguage(code);
        Fonts.OnLanguageChanged();
        Plugin.Instance.OnLanguageChanged();
    }

    public static bool DrawSearchableCombo(string id, string[] labels, ref int selectedIndex, float width, string? hint = null)
        => Dropdown.Draw(id, labels, ref selectedIndex, width, width, searchable: true, hint);

    public static void DrawRangeInline(Configuration cfg, string minId, string maxId, Func<int> getMin, Action<int> setMin, Func<int> getMax, Action<int> setMax, int maxValue, int minValue = 0, string format = "%d s")
    {
        using (PushFrameColors())
        {
            DrawRangeBound(cfg, minId, getMin, setMin, minValue, maxValue, format, (int value) =>
            {
                if (value > getMax())
                {
                    setMax(value);
                }
            });
            DrawRangeDash();
            DrawRangeBound(cfg, maxId, getMax, setMax, minValue, maxValue, format, (int value) =>
            {
                if (value < getMin())
                {
                    setMin(value);
                }
            });
        }
    }

    private static void DrawRangeBound(Configuration cfg, string id, Func<int> getter, Action<int> setter, int minValue, int maxValue, string format, Action<int> onChanged)
    {
        var v = getter();
        ImGui.SetNextItemWidth(RangeDragWidth * ImGuiHelpers.GlobalScale);
        if (!ImGui.DragInt(id, ref v, RangeDragSpeed, minValue, maxValue, format))
        {
            return;
        }
        v = Math.Clamp(v, minValue, maxValue);
        setter(v);
        onChanged(v);
        cfg.SaveDebounced();
    }

    private static void DrawRangeDash()
    {
        var slotWidth = RangeDashSlot * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(0f, 0f);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var dashSize = ImGui.CalcTextSize("-");
        ImGui.SetCursorScreenPos(cursorScreenPos + new Vector2((slotWidth - dashSize.X) * 0.5f, (ImGui.GetFrameHeight() - dashSize.Y) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.TextUnformatted("-");
        }
        ImGui.SameLine(0f, 0f);
        ImGui.SetCursorScreenPos(cursorScreenPos with { X = cursorScreenPos.X + slotWidth });
    }

    public static IDisposable PushFrameColors()
    {
        return ImRaii.PushColor(ImGuiCol.SliderGrab, Styling.AccentGlow).Push(ImGuiCol.SliderGrabActive, Styling.AccentGlowSoft).Push(ImGuiCol.FrameBg, Styling.SliderBg)
            .Push(ImGuiCol.FrameBgHovered, Styling.CardBgHover)
            .Push(ImGuiCol.FrameBgActive, Styling.CardBgHover);
    }
}
