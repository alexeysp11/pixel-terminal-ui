using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.TerminalHack;

public sealed record TerminalHackScreen : TerminalScreen
{
    public required CharacterType CharacterType { get; init; }

    public TerminalHackScreen()
    {
        Name = "TerminalHackScreen";
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

        // Classic Fallout/Cyberpunk style code grid segments memory dump simulation
        TextWidget memoryDumpOne = new()
        {
            Id = Guid.NewGuid(),
            Name = "Dump1",
            Left = 2,
            Top = 3,
            Width = 36,
            Value = "0x4F3A  OVERRIDE  []!$  SYSTEM  //",
            Visible = true
        };

        TextWidget memoryDumpTwo = new()
        {
            Id = Guid.NewGuid(),
            Name = "Dump2",
            Left = 2,
            Top = 4,
            Width = 36,
            Value = "0x4F4C  %#@!^___  LINK  Quantum  *#",
            Visible = true
        };

        TextWidget warningLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "WarningText",
            Left = 2,
            Top = 6,
            Width = 36,
            Value = "PASSWORD REQUIRED. TARGET: OVERRIDE",
            Visible = true
        };

        SubmitHackKeyCommand hackCommand = new();

        TextEntryWidget hackInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "HackInput",
            Left = 2,
            Top = 9,
            Width = 15,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "ENTER DECRYPTED KEYWORD TO ACCESS",
            Visible = true,
            Command = hackCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        hackCommand.WidgetId = hackInput.Id;

        Widgets = [titleLabel, memoryDumpOne, memoryDumpTwo, warningLabel, hackInput];
        FocusedEntryWidgetId = hackInput.Id;
    }
}
