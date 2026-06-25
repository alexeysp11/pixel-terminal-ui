using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.DroneDeployment;

public sealed record DroneDeploymentScreen : TerminalScreen
{
    public required CharacterType CharacterType { get; init; }

    public DroneDeploymentScreen()
    {
        Name = "DroneDeploymentScreen";
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneTitleLabel",
            Left = 6,
            Top = 1,
            Width = 28,
            Value = "--- RCC DRONE LINK ACTIVE ---",
            Visible = true,
            Foreground = ConsoleColor.Gray
        };

        TextWidget statusLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneStatusLabel",
            Left = 2,
            Top = 3,
            Width = 36,
            Value = "PROBE: Recon-01 | STATUS: Hovering",
            Visible = true,
            Foreground = ConsoleColor.Gray
        };

        TextWidget telemetryLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneTelemetryLabel",
            Left = 2,
            Top = 4,
            Width = 36,
            Value = "SIGNAL: 98% | FUEL: Operational",
            Visible = true,
            Foreground = ConsoleColor.Gray
        };

        DeployDroneCommand deployCommand = new()
        {
            State = DroneDeploymentState.AwaitingCommand,
            CharacterType = CharacterType
        };

        TextEntryWidget droneInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "DroneInput",
            Left = 2,
            Top = 7,
            Width = 15,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "ENTER 1 TO SCAN OR 0 TO DISCONNECT",
            Visible = true,
            Command = deployCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        deployCommand.WidgetId = droneInput.Id;

        Widgets = [titleLabel, statusLabel, telemetryLabel, droneInput];
        FocusedEntryWidgetId = droneInput.Id;
    }
}
