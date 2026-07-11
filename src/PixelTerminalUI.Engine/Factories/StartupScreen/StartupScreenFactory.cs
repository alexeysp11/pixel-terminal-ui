using Microsoft.Extensions.Logging;
using PixelTerminalUI.Engine.Screens;

namespace PixelTerminalUI.Engine.Factories.StartupScreen;

/// <summary>
/// A factory responsible for instantiating the entry-point screen for a newly established session,
/// isolating component dependencies within a dedicated service scope.
/// </summary>
public sealed class StartupScreenFactory : IStartupScreenFactory
{
    private readonly ILogger<StartupScreenFactory> _logger;
    private readonly Type _screenType;
    private readonly Func<Type, TerminalScreen> _screenActivator;

    public StartupScreenFactory(ILogger<StartupScreenFactory> logger, Type screenType, Func<Type, TerminalScreen> screenActivator)
    {
        _logger = logger;
        _screenType = screenType;
        _screenActivator = screenActivator;

        // Вот этот гвард-блок вернет стабильность
        if (!typeof(TerminalScreen).IsAssignableFrom(screenType))
        {
            throw new ArgumentException($"The specified type {screenType.Name} must derive from TerminalScreen", nameof(screenType));
        }
    }

    /// <inheritdoc/>
    public TerminalScreen CreateScreen(Guid sessionId)
    {
        // Resolve the transient screen instance cleanly using the high-speed IoC provider delegate
        TerminalScreen screen = _screenActivator(_screenType);

        screen.Id = Guid.NewGuid();
        screen.SessionId = sessionId;
        screen.Visible = true;

        _logger.LogInformation("Instantiated startup screen of type {ScreenType} for Session {SessionId}", _screenType, sessionId);

        return screen;
    }
}
