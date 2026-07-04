using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace TheLostGrid.Server.Scenarios.Welcome;

public sealed record WelcomeScreen : TerminalScreen
{
    public WelcomeScreen()
    {
        Name = "WelcomeScreen";
        Width = 40;
        Height = 12;

        TextWidget welcomeLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "WelcomeLabel",
            Left = 14,
            Top = 2,
            Width = 10,
            Value = "WELCOME TO",
            Visible = true
        };

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "TitleLabel",
            Left = 11,
            Top = 3,
            Width = 17,
            Value = "THE LOST GRID TUI",
            Visible = true,
            Inverted = true
        };

        TextWidget hotkeysLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HotkeysLabel",
            Left = 4,
            Top = 5,
            Width = 31,
            Value = "-q: Quit  -h: Help  -b: Back",
            Visible = true,
            Foreground = ConsoleColor.DarkCyan
        };

        TextWidget actionLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ActionLabel",
            Left = 2,
            Top = 7,
            Width = 32,
            Value = "Press ENTER to connect link...",
            Visible = true,
            Foreground = ConsoleColor.Gray
        };

        WelcomeStartGameCommand connectionCommand = new();

        TextEntryWidget hiddenInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "ConnectionTriggerInput",
            Left = 2,
            Top = 8,
            Width = 36,
            Required = false,
            EmptyEnterSymbol = '.',
            Hint = "CONNECT TO NEURAL NETWORK",
            Visible = true,
            Command = connectionCommand,
            Value = string.Empty
        };

        // Link the instantiated command back to its hosting widget layout identity
        connectionCommand.WidgetId = hiddenInput.Id;

        Widgets = [welcomeLabel, titleLabel, hotkeysLabel, actionLabel, hiddenInput];
        FocusedEntryWidgetId = hiddenInput.Id;
    }
}
