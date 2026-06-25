using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.SectorNavigation;

public sealed record SectorNavigationScreen : TerminalScreen
{
    public CharacterType CharacterType { get; init; }

    public SectorNavigationScreen(CharacterType characterType)
    {
        if (characterType is CharacterType.None)
            throw new InvalidOperationException("Incorrect character type");
        CharacterType = characterType;

        Name = "SectorNavigationScreen";
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HubTitleLabel",
            Left = 8,
            Top = 1,
            Width = 32,
            Value = "SECTOR NAVIGATION HUB",
            Visible = true
        };

        TextWidget optionOneLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "OptionOneLabel",
            Left = 2,
            Top = 3,
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
            Top = 4,
            Width = 35,
            Value = "[2] SCAN DEEP NET SECTORS",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        ExploreSectorCommand exploreCommand = new() { CharacterType = characterType };

        TextEntryWidget navigationInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "NavigationInput",
            Left = 2,
            Top = 7,
            Width = 10,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "ENTER OPTION CODE AND PRESS ENTER",
            Visible = true,
            Command = exploreCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        // Bind the tracking context back to the command configuration layout mapping
        exploreCommand.WidgetId = navigationInput.Id;

        Widgets = [titleLabel, optionOneLabel, optionTwoLabel, navigationInput];
        FocusedEntryWidgetId = navigationInput.Id;
    }
}
