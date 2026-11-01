using System;
using AutoTripleTriadGrind.Core.Tasks;

namespace AutoTripleTriadGrind.Windows.Sections;

internal static class RunWorkload
{
    public readonly record struct Workload(int Done, int Needed)
    {
        public float Fraction
        {
            get
            {
                if (Needed <= 0)
                {
                    return 0f;
                }
                return Math.Clamp((float)Done / (float)Needed, 0f, 1f);
            }
        }
    }

    public static Workload Measure(AutoTriadController controller)
    {
        var sessionSnapshot = controller.SessionSnapshot;
        if (sessionSnapshot == null)
        {
            return default;
        }
        if (sessionSnapshot.Mode == TriadRunMode.Farm)
        {
            var configuration = Plugin.Instance.Configuration;
            if (configuration.FarmStop != FarmStopKind.Matches)
            {
                return default;
            }
            return new Workload(sessionSnapshot.MatchesPlayed, Math.Max(1, configuration.FarmMatchLimit) * Math.Max(1, controller.Progress.Queue.Count));
        }
        var progress = controller.Progress;
        return new Workload(progress.QueueNext, progress.Queue.Count);
    }
}
