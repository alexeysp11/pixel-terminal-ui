using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace TheLostGrid.Server.Scenarios.Help;

public sealed record HelpScreen : TerminalScreen
{
    public HelpScreen()
    {
        Name = nameof(HelpScreen);
        Width = 42;
        Height = 20;
        Widgets =
        [
            new TextWidget { Id = Guid.NewGuid(), Name = "ShortcutsTitleLabel", Left = 15, Top = 2, Value = "SHORTCUTS" },
            new TextWidget { Id = Guid.NewGuid(), Name = "QuitShortcutLabel", Left = 4, Top = 5, Value = "-q: Quit" },
            new TextWidget { Id = Guid.NewGuid(), Name = "NextShortcutLabel", Left = 4, Top = 6, Value = "-n: Next"},
            new TextWidget { Id = Guid.NewGuid(), Name = "BackShortcutLabel", Left = 4, Top = 7, Value = "-b: Back"},
            new TextWidget { Id = Guid.NewGuid(), Name = "MenuShortcutLabel", Left = 4, Top = 8, Value = "-m: Menu"},
            new TextWidget { Id = Guid.NewGuid(), Name = "HelpShortcutLabel", Left = 4, Top = 9, Value = "-h: Help"},
            new TextWidget { Id = Guid.NewGuid(), Name = "CancelShortcutLabel", Left = 4, Top = 10, Value = "-x: Cancel"},
            new TextWidget { Id = Guid.NewGuid(), Name = "InfoShortcutLabel", Left = 4, Top = 11, Value = "-info: Screen Context Info"},

            new TextEntryWidget
            {
                Id = Guid.NewGuid(),
                Name = "EnterShortcutWidget",
                Value = string.Empty,
                Left = 2,
                Top = 14,
                Width = 38,
                Hint = "PRESS ENTER TO RETURN",
                Command = new DismissHelpCommand()
            }
        ];
    }
}
