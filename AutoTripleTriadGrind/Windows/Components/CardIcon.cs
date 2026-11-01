using System.Numerics;
using AutoTripleTriadGrind.Core.Triad.Data;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class CardIcon
{
    public enum State : byte
    {
        Missing,
        Selected,
        Owned
    }

    public static void Draw(ImDrawListPtr drawList, ushort cardId, Vector2 min, float size, State state, float hover = 0f)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var max = min + new Vector2(size, size);
        var rounding = 6f * globalScale;
        var card = TriadData.Set.Cards[cardId];
        var owned = state == State.Owned;
        if (Svc.Texture.GetFromGameIcon(new GameIconLookup(card.SmallIconId)).TryGetWrap(out var texture, out var _))
        {
            var color = (owned ? new Vector4(1f, 1f, 1f, 0.38f) : Vector4.One);
            drawList.AddImageRounded(texture.Handle, min, max, Vector2.Zero, Vector2.One, Paint.Col(color), rounding);
        }
        else
        {
            Paint.Fill(drawList, min, max, Styling.Surface2, rounding);
        }
        var borderColor = state switch
        {
            State.Selected => Styling.AccentGlow,
            State.Owned => Styling.WithAlpha(Styling.AccentMint, 0.55f),
            _ => Styling.WithAlpha(Styling.BorderDim, 0.8f + 0.2f * hover),
        };
        Paint.Stroke(drawList, min, max, borderColor, rounding, (state == State.Selected) ? (2f * globalScale) : (1f * globalScale));
        if (hover > 0.01f && !owned)
        {
            Paint.Fill(drawList, min, max, Styling.WithAlpha(Styling.Highlight, 1.5f * hover), rounding);
        }
        if (owned)
        {
            var ownedBadgeCenter = new Vector2(max.X - size * 0.2f, max.Y - size * 0.2f);
            var ownedBadgeRadius = size * 0.17f;
            drawList.AddCircleFilled(ownedBadgeCenter, ownedBadgeRadius, Paint.Col(Styling.AccentMint));
            Paint.Check(drawList, ownedBadgeCenter, ownedBadgeRadius * 1.1f, Styling.InkOnGlow, 1.6f * globalScale);
        }
        else if (state == State.Selected)
        {
            var selectedBadgeCenter = new Vector2(max.X - size * 0.2f, min.Y + size * 0.2f);
            var selectedBadgeRadius = size * 0.17f;
            drawList.AddCircleFilled(selectedBadgeCenter, selectedBadgeRadius, Paint.Col(Styling.AccentGlow));
            Paint.Check(drawList, selectedBadgeCenter, selectedBadgeRadius * 1.1f, Styling.InkOnGlow, 1.6f * globalScale);
        }
    }
}
