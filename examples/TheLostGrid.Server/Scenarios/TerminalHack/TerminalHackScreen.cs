using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Widgets;
using TheLostGrid.Server.Domain.Enums;

namespace TheLostGrid.Server.Scenarios.TerminalHack;

public sealed record TerminalHackScreen : TerminalScreen
{
    public int Energy { get; init; }
    public int Credits { get; init; }
    public int AttemptsLeft { get; set; }
    public string TargetHash { get; init; }
    public string[] ActiveHashes { get; init; }
    public CharacterType CharacterType { get; init; }

    public TerminalHackScreen(
        CharacterType characterType,
        int energy,
        int credits,
        int attemptsLeft,
        string targetHash,
        string[] activeHashes)
    {
        CharacterType = characterType;
        Energy = energy;
        Credits = credits;
        AttemptsLeft = attemptsLeft;
        TargetHash = targetHash;
        ActiveHashes = activeHashes;

        Name = nameof(TerminalHackScreen);
        Width = 40;
        Height = 13;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HackTitle",
            Left = 4,
            Top = 1,
            Width = 32,
            Value = "== TERMINAL MATRIX BRUTEFORCE ==",
            Visible = true
        };

        string attemptsIndicator = $"ATTEMPTS LEFT: {(attemptsLeft > 1 ? "[X] [X]" : "[X] [ ]")}";
        int attemptsLeftOffset = (Width - attemptsIndicator.Length) / 2;

        TextWidget attemptsLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HackAttemptsLabel",
            Left = attemptsLeftOffset,
            Top = 2,
            Width = attemptsIndicator.Length,
            Value = attemptsIndicator,
            Visible = true,
            Foreground = ConsoleColor.Red
        };

        string hash1 = activeHashes.Length > 0 ? activeHashes[0] : "XXXXXXXX";
        string hash2 = activeHashes.Length > 1 ? activeHashes[1] : "XXXXXXXX";
        string hash3 = activeHashes.Length > 2 ? activeHashes[2] : "XXXXXXXX";

        // Prepend numeric selectors to completely bypass clipboard copy constraints inside Docker buffers
        TextWidget memoryDumpOne = new()
        {
            Id = Guid.NewGuid(),
            Name = "Dump1",
            Left = 2,
            Top = 4,
            Width = 36,
            Value = $" [1] 0x4F3A  {hash1}  SYSTEM  //",
            Visible = true,
            Foreground = ConsoleColor.DarkGreen
        };

        TextWidget memoryDumpTwo = new()
        {
            Id = Guid.NewGuid(),
            Name = "Dump2",
            Left = 2,
            Top = 5,
            Width = 36,
            Value = $" [2] 0x4F4C  {hash2}  LINK    *#",
            Visible = true,
            Foreground = ConsoleColor.DarkGreen
        };

        TextWidget memoryDumpThree = new()
        {
            Id = Guid.NewGuid(),
            Name = "Dump3",
            Left = 2,
            Top = 6,
            Width = 36,
            Value = $" [3] 0x4F60  {hash3}  QUANTUM ==",
            Visible = true,
            Foreground = ConsoleColor.DarkGreen
        };

        TextWidget warningLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "WarningText",
            Left = 2,
            Top = 8,
            Width = 36,
            Value = $"CRITICAL RESOURCE NET: ENG {energy}%",
            Visible = true,
            Foreground = ConsoleColor.Yellow
        };

        TerminalHackSubmitKeyCommand hackCommand = new();

        TextEntryWidget hackInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "HackInput",
            Left = 2,
            Top = 10,
            Width = 15,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "ENTER OPTION CODE TO ACCESS",
            Visible = true,
            Command = hackCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        hackCommand.WidgetId = hackInput.Id;

        Widgets = [titleLabel, attemptsLabel, memoryDumpOne, memoryDumpTwo, memoryDumpThree, warningLabel, hackInput];
        FocusedEntryWidgetId = hackInput.Id;
    }
}
