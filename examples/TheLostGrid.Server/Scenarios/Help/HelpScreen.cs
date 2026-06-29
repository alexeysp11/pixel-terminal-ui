using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace TheLostGrid.Server.Scenarios.Help;

public sealed record HelpScreen : TerminalScreen
{
    public HelpScreen()
    {
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HelpTitleLabel",
            Left = 15,
            Top = 1,
            Width = 9,
            Value = "SHORTCUTS",
            Visible = true,
            Inverted = true
        };

        TextWidget row1 = new()
        {
            Id = Guid.NewGuid(),
            Name = "HelpRow1",
            Left = 5,
            Top = 3,
            Width = 30,
            Value = "-q: Quit        -x: Cancel",
            Visible = true,
            Foreground = ConsoleColor.DarkCyan
        };

        TextWidget row2 = new()
        {
            Id = Guid.NewGuid(),
            Name = "HelpRow2",
            Left = 5,
            Top = 4,
            Width = 30,
            Value = "-b: Back        -h: Help",
            Visible = true,
            Foreground = ConsoleColor.DarkCyan
        };

        TextWidget row3 = new()
        {
            Id = Guid.NewGuid(),
            Name = "HelpRow3",
            Left = 5,
            Top = 5,
            Width = 30,
            Value = "-n: Next",
            Visible = true,
            Foreground = ConsoleColor.DarkCyan
        };

        TextWidget actionLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "HelpActionLabel",
            Left = 7,
            Top = 7,
            Width = 26,
            Value = "PRESS ENTER TO EXIT HELP",
            Visible = true,
            Foreground = ConsoleColor.Gray
        };

        DismissHelpCommand dismissCommand = new();

        TextEntryWidget exitInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "HelpExitInput",
            Left = 2,
            Top = 8,
            Width = 36,
            Required = false,
            EmptyEnterSymbol = '.',
            Hint = "RETURN TO GRID TERMINAL",
            Visible = true,
            Command = dismissCommand,
            Value = string.Empty
        };

        // Link the instantiated command back to its hosting widget layout identity
        dismissCommand.WidgetId = exitInput.Id;

        Widgets = [titleLabel, row1, row2, row3, actionLabel, exitInput];
        FocusedEntryWidgetId = exitInput.Id;
    }
}
