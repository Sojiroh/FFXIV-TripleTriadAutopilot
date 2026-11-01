using System;
using System.Diagnostics;
using Dalamud.Bindings.ImGui;

namespace AutoTripleTriadGrind.Windows;

internal static class UrlActions
{
    public static void OpenInBrowser(string url, Action<Exception>? onError = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            ImGui.SetClipboardText(url);
            onError?.Invoke(exception);
        }
    }
}
