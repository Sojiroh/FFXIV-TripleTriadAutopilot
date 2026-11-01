namespace AutoTripleTriadGrind.Core.Tasks;

internal enum TriadPhase : byte
{
    Idle,
    Registering,
    Planning,
    Optimizing,
    Travelling,
    Challenging,
    Playing,
    Rematch,
    Finishing,
    Paused
}
