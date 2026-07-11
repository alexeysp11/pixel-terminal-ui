using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Widgets;
using TheLostGrid.Server.Domain.Enums;

namespace TheLostGrid.Server.Scenarios.PowerGridTerminal;

public sealed record PowerGridTerminalScreen : TerminalScreen
{
    public CharacterType CharacterType { get; init; }
    public int Energy { get; init; }
    public int Credits { get; init; }

    public PowerGridTerminalScreen(CharacterType characterType, int energy, int credits)
    {
        CharacterType = characterType;
        Energy = energy;
        Credits = credits;

        Name = nameof(PowerGridTerminalScreen);
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "PowerGridTitle",
            Left = 8,
            Top = 1,
            Width = 24,
            Value = "== POWER GRID TERMINAL ==",
            Visible = true,
            Inverted = true
        };

        string statusText = $"ENG: {energy}% | CR: {credits}";
        int calculatedLeftOffset = (Width - statusText.Length) / 2;

        TextWidget statusLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "PowerGridStatus",
            Left = calculatedLeftOffset,
            Top = 2,
            Width = statusText.Length,
            Value = statusText,
            Visible = true,
            Foreground = ConsoleColor.Cyan
        };

        TextWidget rateLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "PowerGridRate",
            Left = 2,
            Top = 4,
            Width = 36,
            Value = " EXCHANGE RATE: 10 CR => +25 ENG",
            Visible = true,
            Foreground = ConsoleColor.DarkYellow
        };

        TextWidget optionLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "PowerGridOption",
            Left = 2,
            Top = 6,
            Width = 35,
            Value = " [1] BUY ENERGY  [0] RETURN TO HUB",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        PowerGridTerminalBuyEnergyCommand buyCommand = new() { CharacterType = characterType };

        TextEntryWidget gridInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "PowerGridInput",
            Left = 2,
            Top = 9,
            Width = 10,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "ENTER COMMAND CODE AND PRESS ENTER",
            Visible = true,
            Command = buyCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        buyCommand.WidgetId = gridInput.Id;

        Widgets = [titleLabel, statusLabel, rateLabel, optionLabel, gridInput];
        FocusedEntryWidgetId = gridInput.Id;
    }
}
