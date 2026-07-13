using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Widgets;
using TheLostGrid.Server.Domain.Enums;

namespace TheLostGrid.Server.Scenarios.SectorNavigation;

public sealed record SectorNavigationScreen : TerminalScreen
{
    public CharacterType CharacterType { get; init; }
    public int Energy { get; init; }
    public int Credits { get; init; }

    public SectorNavigationScreen(CharacterType characterType, int energy, int credits)
    {
        if (characterType is CharacterType.None)
        {
            throw new InvalidOperationException("Incorrect character type");
        }

        CharacterType = characterType;
        Energy = energy;
        Credits = credits;

        Name = nameof(SectorNavigationScreen);
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HubTitleLabel",
            Left = 9,
            Top = 1,
            Width = 21,
            Value = "SECTOR NAVIGATION HUB",
            Visible = true
        };

        // Construct a dynamic status string containing real-time snapshot data from the persistence layer
        string statusText = $"ENG: {energy}% | CR: {credits}";
        int calculatedLeftOffset = (Width - statusText.Length) / 2;

        TextWidget statusLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HubStatusLabel",
            Left = calculatedLeftOffset,
            Top = 2,
            Width = statusText.Length,
            Value = statusText,
            Visible = true,
            Foreground = ConsoleColor.Cyan
        };

        TextWidget optionOneLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "OptionOneLabel",
            Left = 2,
            Top = 4,
            Width = 35,
            Value = characterType is CharacterType.Hacker ? "[1] INITIATE TERMINAL HACK" : "[1] DEPLOY RECON DRONE",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        TextWidget optionTwoLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "OptionTwoLabel",
            Left = 2,
            Top = 5,
            Width = 35,
            Value = "[2] SCAN DEEP NET SECTORS",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        TextWidget optionThreeLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "OptionTwoLabel",
            Left = 2,
            Top = 6,
            Width = 35,
            Value = "[3] BUY ENERGY",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        SectorNavigationExploreCommand exploreCommand = new() { CharacterType = characterType };

        TextEntryWidget navigationInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "NavigationInput",
            Left = 2,
            Top = 8,
            Width = 10,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "ENTER OPTION CODE AND PRESS ENTER",
            Visible = true,
            Command = exploreCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        exploreCommand.WidgetId = navigationInput.Id;

        // Include the newly constructed telemetry status label into the active viewport layout collection
        Widgets = [titleLabel, statusLabel, optionOneLabel, optionTwoLabel, optionThreeLabel, navigationInput];
        FocusedEntryWidgetId = navigationInput.Id;
    }
}
