using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace TheLostGrid.Server.Scenarios.CharacterCreation;

public sealed record CharacterCreationScreen : TerminalScreen
{
    public CharacterCreationScreen()
    {
        Name = "CharacterCreationScreen";
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "TitleLabel",
            Left = 8,
            Top = 1,
            Width = 24,
            Value = "INITIALIZING NEURAL LINK",
            Visible = true,
            Foreground = ConsoleColor.Gray
        };

        // First Field: Operator Name Layout Block
        TextWidget nameLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "NameLabel",
            Left = 2,
            Top = 3,
            Width = 20,
            Value = "Enter Operator Name:",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        TextEntryWidget nameInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "OperatorNameInput",
            Left = 2,
            Top = 4,
            Width = 15,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "TYPE CODE-NAME AND PRESS ENTER",
            Visible = true,
            Value = string.Empty,
            TabIndex = 1
        };

        // Second Field: Neural Class Layout Block
        TextWidget classLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ClassLabel",
            Left = 2,
            Top = 6,
            Width = 35,
            Value = "Choose Class (HACKER/RIGGER):",
            Visible = true,
            Foreground = ConsoleColor.DarkGray
        };

        RegisterOperatorCommand registrationCommand = new();

        TextEntryWidget classInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "NeuralClassInput",
            Left = 2,
            Top = 7,
            Width = 15,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "TYPE CLASS TO BOOT SYSTEM",
            Visible = true,
            Command = registrationCommand,
            Value = string.Empty,
            TabIndex = 2
        };

        // Link the command back to its hosting widget layout wrapper instance checkpoint
        registrationCommand.WidgetId = classInput.Id;

        Widgets = [titleLabel, nameLabel, nameInput, classLabel, classInput];
        FocusedEntryWidgetId = nameInput.Id;
    }
}
