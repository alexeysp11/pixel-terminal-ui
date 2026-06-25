using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace TheLostGrid.Server.Scenarios.SecurityQuarantine;

public sealed record SecurityQuarantineScreen : TerminalScreen
{
    public SecurityQuarantineScreen()
    {
        Name = "SecurityQuarantineScreen";
        Width = 40;
        Height = 12;

        TextWidget alertLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "AlertLabel",
            Left = 2,
            Top = 1,
            Width = 36,
            Value = "!! QUARANTINE: INTRUSION DETECTED !!",
            Visible = true
        };

        TextWidget instructionLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "InstructionLabel",
            Left = 2,
            Top = 4,
            Width = 36,
            Value = "Purge logs to escape. Code: FLUSH",
            Visible = true
        };

        TextEntryWidget purgeInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "PurgeInput",
            Left = 2,
            Top = 6,
            Width = 10,
            Required = true,
            EmptyEnterSymbol = '!',
            Hint = "Type command to clear your trace logs",
            Visible = true,
            Command = new PurgeLogsCommand(),
            Value = string.Empty
        };

        Widgets = [alertLabel, instructionLabel, purgeInput];
        FocusedEntryWidgetId = purgeInput.Id;
    }
}
