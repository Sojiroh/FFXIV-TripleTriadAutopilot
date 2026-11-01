namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal readonly record struct BoardFrame(bool CanAct, bool PlayerTurn, byte TurnState, bool BannerVisible, int CardsInHands, bool IsPvP, bool ReadFailed);
