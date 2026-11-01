using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommons.Reflection;

namespace AutoTripleTriadGrind.Core.External;

public static class PluginInstaller
{
    private static readonly HashSet<ExternalPlugin> InFlight = new HashSet<ExternalPlugin>();

    public static bool IsInstalling(ExternalPlugin plugin)
    {
        return InFlight.Contains(plugin);
    }

    public static async Task<bool> Install(ExternalPlugin plugin)
    {
        if (!InFlight.Add(plugin))
        {
            return false;
        }
        try
        {
            var info = ExternalPlugins.Catalog[plugin];
            RunLog.Info("Installing " + info.DisplayName + " from " + info.RepoUrl);
            var installed = await DalamudReflector.AddPlugin(info.RepoUrl, info.InternalName);
            RunLog.Info(installed ? (info.DisplayName + " installed.") : (info.DisplayName + " install reported failure; the repo may need to be added manually."));
            return installed;
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, $"Installing {plugin} threw");
            return false;
        }
        finally
        {
            InFlight.Remove(plugin);
        }
    }
}
