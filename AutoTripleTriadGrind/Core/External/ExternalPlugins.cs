using System;
using System.Collections.Generic;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.External;

public static class ExternalPlugins
{
    private static readonly ExternalPlugin[] all = new ExternalPlugin[1];

    public static readonly IReadOnlyDictionary<ExternalPlugin, ExternalPluginInfo> Catalog = new Dictionary<ExternalPlugin, ExternalPluginInfo> { [ExternalPlugin.Vnavmesh] = new ExternalPluginInfo("vnavmesh", "vnavmesh", "https://puni.sh/api/repository/veyn", "Pathfinding, flying, and movement to Triple Triad NPCs.", Required: true) };

    public static IReadOnlyList<ExternalPlugin> All => all;

    public static bool IsInstalled(ExternalPlugin plugin)
    {
        var info = Catalog[plugin];
        foreach (var installedPlugin in Svc.PluginInterface.InstalledPlugins)
        {
            if (installedPlugin.IsLoaded)
            {
                if (installedPlugin.InternalName == info.InternalName)
                {
                    return true;
                }
                if (info.Aliases != null && Array.IndexOf(info.Aliases, installedPlugin.InternalName) >= 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool AllRequiredInstalled()
    {
        for (int i = 0; i < all.Length; i++)
        {
            if (Catalog[all[i]].Required && !IsInstalled(all[i]))
            {
                return false;
            }
        }
        return true;
    }

    public static string MissingRequiredNames()
    {
        var names = new List<string>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            var info = Catalog[all[i]];
            if (info.Required && !IsInstalled(all[i]))
            {
                names.Add(info.DisplayName);
            }
        }
        return string.Join(", ", names);
    }
}
