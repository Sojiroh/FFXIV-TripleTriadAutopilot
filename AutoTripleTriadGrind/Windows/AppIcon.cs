using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Windows;

internal static class AppIcon
{
    private const string FileName = "Icon.png";

    private static string? path;

    private static bool? exists;

    public static void Draw(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, float alpha = 1f)
    {
        if (path == null)
        {
            path = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty, "Images", FileName);
        }
        var fileExists = exists == true;
        if (!exists.HasValue)
        {
            fileExists = File.Exists(path);
            exists = fileExists;
        }
        if (exists.Value)
        {
            var iconTexture = Svc.Texture.GetFromFile(path).GetWrapOrEmpty();
            dl.AddImageRounded(iconTexture.Handle, min, max, Vector2.Zero, Vector2.One, Paint.Col(new Vector4(1f, 1f, 1f, alpha)), rounding, ImDrawFlags.RoundCornersAll);
        }
        else
        {
            Paint.Gradient(dl, min, max, Styling.AccentGlowSoft, Styling.AccentGlow, rounding);
            TextDraw.IconCentered(FontAwesomeIcon.Crosshairs, (min + max) * 0.5f, Styling.WithAlpha(Styling.TextStrong, alpha));
        }
    }
}
