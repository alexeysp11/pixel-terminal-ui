using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Models;

public sealed record GameSessionState(
    string Name,
    CharacterType CharacterType,
    int Energy = 100,
    int Credits = 50,
    string TargetHash = "",
    string[]? ActiveHashes = null,
    int AttemptsLeft = 2
);
