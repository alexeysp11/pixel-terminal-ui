using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.DroneDeployment;

public sealed record DroneDeploymentScreen : TerminalScreen
{
    public CharacterType CharacterType { get; init; }

    public int Energy { get; init; }

    public int Credits { get; init; }

    public DroneDeploymentScreen(CharacterType characterType, int energy, int credits)
    {
        CharacterType = characterType;
        Energy = energy;
        Credits = credits;

        Name = nameof(DroneDeploymentScreen);
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneTitleLabel",
            Left = 8,
            Top = 1,
            Width = 24,
            Value = "== DRONE BAY CONTROL ==",
            Visible = true
        };

        string statusText = $"ENG: {energy}% | CR: {credits}";
        int calculatedLeftOffset = (Width - statusText.Length) / 2;

        TextWidget statusLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneStatusLabel",
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
            Name = "DroneOption1",
            Left = 2,
            Top = 4,
            Width = 35,
            Value = "[1] DEPLOY RECON DRONE (-10 ENG)",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        TextWidget optionZeroLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneOption0",
            Left = 2,
            Top = 5,
            Width = 35,
            Value = "[0] RETURN TO NAVIGATION HUB",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        DeployDroneCommand deployCommand = new() { CharacterType = characterType };

        TextEntryWidget droneInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneInput",
            Left = 2,
            Top = 8,
            Width = 10,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "SELECT DIRECTIVE CODE AND PRESS ENTER",
            Visible = true,
            Command = deployCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        deployCommand.WidgetId = droneInput.Id;

        Widgets = [titleLabel, statusLabel, optionOneLabel, optionZeroLabel, droneInput];
        FocusedEntryWidgetId = droneInput.Id;
    }
}
