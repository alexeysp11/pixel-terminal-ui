using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.SectorScanner;

public sealed record SectorScannerScreen : TerminalScreen
{
    public required CharacterType CharacterType { get; init; }

    public SectorScannerScreen()
    {
        Name = "SectorScannerScreen";
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScannerTitleLabel",
            Left = 4,
            Top = 1,
            Width = 32,
            Value = "--- DEEP NET RADAR ACTIVE ---",
            Visible = true
        };

        TextWidget statusLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScannerStatus",
            Left = 2,
            Top = 3,
            Width = 35,
            Value = "Scanning signatures... Clear.",
            Visible = true
        };

        TextWidget backLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "BackLabel",
            Left = 2,
            Top = 5,
            Width = 35,
            Value = " [0] RETURN TO MAIN HUB",
            Visible = true
        };

        // We reuse or target the specific machine-state command
        ScanSectorsCommand scanCommand = new()
        {
            CharacterType = CharacterType,
            CurrentStep = ScannerStep.AwaitingScreenResponse
        };

        TextEntryWidget scannerInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScannerInput",
            Left = 2,
            Top = 8,
            Width = 10,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "ENTER 0 TO DISCONNECT LINK",
            Visible = true,
            Command = scanCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        scanCommand.WidgetId = scannerInput.Id;

        Widgets = [titleLabel, statusLabel, backLabel, scannerInput];
        FocusedEntryWidgetId = scannerInput.Id;
    }
}
