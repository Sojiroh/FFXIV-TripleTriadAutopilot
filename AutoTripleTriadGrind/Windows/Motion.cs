using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows;

internal static class Motion
{
    private readonly record struct Switch(int State, long Tick);

    public const float SwitchMs = 240f;

    public const float SwitchSlide = 10f;

    private static readonly Dictionary<int, float> values = new();

    private static readonly Dictionary<int, Switch> switches = new();

    public static bool Reduced => Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion;

    public static float DeltaTime => MathF.Min(ImGui.GetIO().DeltaTime, 0.05f);

    public static int Key(string id)
    {
        return (int)ImGui.GetID(id);
    }

    public static int Key(string id, int salt)
    {
        return HashCode.Combine(ImGui.GetID(id), salt);
    }

    public static int Key(string id, uint salt)
    {
        return HashCode.Combine(ImGui.GetID(id), salt);
    }

    public static float Approach(int key, float target, float speed = 14f)
    {
        if (Reduced || !values.TryGetValue(key, out var current))
        {
            values[key] = target;
            return target;
        }
        var next = current + (target - current) * (1f - MathF.Exp(-speed * DeltaTime));
        if (MathF.Abs(next - target) < 0.0005f)
        {
            next = target;
        }
        values[key] = next;
        return next;
    }

    public static float Hover(int key, bool hovered)
    {
        return Approach(key, hovered ? 1f : 0f, 18f);
    }

    public static float Reveal(long startedTick, float durationMs, float delayMs = 0f)
    {
        if (Reduced)
        {
            return 1f;
        }
        return EaseOutCubic(Math.Clamp(((float)(Environment.TickCount64 - startedTick) - delayMs) / durationMs, 0f, 1f));
    }

    public static float Transition(int key, bool state, float durationMs = SwitchMs)
    {
        return Transition(key, state ? 1 : 0, durationMs);
    }

    public static float Transition(int key, int state, float durationMs = SwitchMs)
    {
        if (!switches.TryGetValue(key, out var entry))
        {
            switches[key] = new Switch(state, 0L);
            return 1f;
        }
        if (entry.State != state)
        {
            entry = new Switch(state, Environment.TickCount64);
            switches[key] = entry;
        }
        return Reveal(entry.Tick, durationMs);
    }

    public static ImRaii.StyleDisposable PushAlpha(float progress)
    {
        return ImRaii.PushStyle(ImGuiStyleVar.Alpha, MathF.Max(0.001f, progress * ImGui.GetStyle().Alpha));
    }

    public static ImRaii.StyleDisposable PushReveal(float progress, float slide = SwitchSlide)
    {
        if (progress < 1f)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (1f - progress) * slide * ImGuiHelpers.GlobalScale);
        }
        return PushAlpha(progress);
    }

    public static ImRaii.StyleDisposable PushSwitch(string id, bool state, float durationMs = SwitchMs, float slide = SwitchSlide)
    {
        return PushReveal(Transition(Key(id), state, durationMs), slide);
    }

    public static ImRaii.StyleDisposable PushSwitch(string id, int state, float durationMs = SwitchMs, float slide = SwitchSlide)
    {
        return PushReveal(Transition(Key(id), state, durationMs), slide);
    }

    public static ImRaii.StyleDisposable? PushSection(string id, bool shown, float durationMs = SwitchMs, float slide = SwitchSlide)
    {
        var progress = Transition(Key(id), shown, durationMs);
        if (!shown)
        {
            return null;
        }
        return PushReveal(progress, slide);
    }

    public static float EaseOutCubic(float t)
    {
        var inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    public static float EaseInOutCubic(float t)
    {
        if (!(t < 0.5f))
        {
            return 1f - MathF.Pow(-2f * t + 2f, 3f) * 0.5f;
        }
        return 4f * t * t * t;
    }

    public static float EaseOutBack(float t)
    {
        var shifted = t - 1f;
        return 1f + 2.70158f * shifted * shifted * shifted + 1.70158f * shifted * shifted;
    }

    public static float Smoothstep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public static float Wave(double periodMs)
    {
        return MathF.Sin(Styling.Phase(periodMs) * (float)Math.PI * 2f);
    }
}
