using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.SecurityQuarantine;

public sealed class PurgeLogsCommand : Command<PurgeLogsState>
{
    public override PurgeLogsState State { get; set; } = PurgeLogsState.None;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        string secureToken = context.InputValue.Trim().ToUpperInvariant();

        // The player must type the exact cryptographic bypass sequence token string to unlock the quarantine jail
        if (secureToken == "FLUSH")
        {
            // Escape routine successful: telemetry routes reset operator back into the safe starting sector hub
            SectorNavigationScreen safeScreen = new(CharacterType.None)
            {
                Id = Guid.NewGuid(),
                Name = nameof(SectorNavigationScreen),
                SessionId = context.SessionId
            };

            // Transit execution loop clears, operator escapes the quarantine sector layer
            return ValueTask.FromResult(true);
        }

        // Failed attempt: Operator entered incorrect clear token sequence, trigger error layout alert loops
        return ValueTask.FromResult(false);
    }
}
