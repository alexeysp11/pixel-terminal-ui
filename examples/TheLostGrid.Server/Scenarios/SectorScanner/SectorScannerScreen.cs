using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Widgets;
using TheLostGrid.Server.Domain.Enums;

namespace TheLostGrid.Server.Scenarios.SectorScanner;

public sealed record SectorScannerScreen : TerminalScreen
{
    public CharacterType CharacterType { get; init; }
    public int Energy { get; init; }
    public int Credits { get; init; }
    public string ScanResultLog { get; init; }

    public SectorScannerScreen(CharacterType characterType, int energy, int credits, string scanResultLog)
    {
        CharacterType = characterType;
        Energy = energy;
        Credits = credits;
        ScanResultLog = scanResultLog;

        Name = nameof(SectorScannerScreen);
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

        string statusText = $"ENG: {energy}% | CR: {credits}";
        int calculatedLeftOffset = (Width - statusText.Length) / 2;

        TextWidget statusLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScannerTelemetryStatus",
            Left = calculatedLeftOffset,
            Top = 2,
            Width = statusText.Length,
            Value = statusText,
            Visible = true,
            Foreground = ConsoleColor.Cyan
        };

        // Parse and safely segment the incoming text log to extract the primary event state
        string firstLineText = scanResultLog.Contains('.')
            ? scanResultLog[..(scanResultLog.IndexOf('.') + 1)]
            : scanResultLog;

        TextWidget statusLogLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScannerStatusLine1",
            Left = 2,
            Top = 4,
            Width = 36,
            Value = firstLineText,
            Visible = true,
            Foreground = ConsoleColor.Green
        };

        // Extract the trailing computational reward metric or fallback gracefully to an empty block
        string secondLineText = scanResultLog.Contains('.')
            ? scanResultLog[(scanResultLog.IndexOf('.') + 1)..].Trim()
            : string.Empty;

        TextWidget rewardLogLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScannerStatusLine2",
            Left = 2,
            Top = 5,
            Width = 36,
            Value = secondLineText,
            Visible = !string.IsNullOrEmpty(secondLineText),
            Foreground = ConsoleColor.Yellow
        };

        TextWidget backLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "BackLabel",
            Left = 2,
            Top = 7, // Shifted downward by one unit to accommodate the dual line text layout
            Width = 35,
            Value = " [0] RETURN TO MAIN HUB",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        SectorScannerScanCommand scanCommand = new()
        {
            CharacterType = characterType
        };

        TextEntryWidget scannerInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScannerInput",
            Left = 2,
            Top = 9,
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

        // Bind the newly expanded dynamic widget ecosystem components into the active viewport layout
        Widgets = [titleLabel, statusLabel, statusLogLabel, rewardLogLabel, backLabel, scannerInput];
        FocusedEntryWidgetId = scannerInput.Id;
    }
}
