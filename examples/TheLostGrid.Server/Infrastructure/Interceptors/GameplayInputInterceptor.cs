using PixelTerminalUI.Engine.Repositories;
using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.SymbolHandling;
using TheLostGrid.Server.Scenarios.Help;

namespace TheLostGrid.Server.Infrastructure.Interceptors;

/// <summary>
/// Provides domain-specific user input interception routines for the specialized terminal gameplay pipeline.
/// </summary>
public sealed class GameplayInputInterceptor
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameplayInputInterceptor"/> class.
    /// </summary>
    /// <param name="scopeFactory">The infrastructure service scope factory used to dynamically resolve scoped dependencies.</param>
    public GameplayInputInterceptor(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Evaluates structural screen context parameters against incoming textual frames to intercept global help request triggers.
    /// </summary>
    /// <param name="screen">The active presentation layer terminal screen model configuration instance.</param>
    /// <param name="userInput">The raw text sequence captured from the thin client interaction input boundary buffer frame.</param>
    /// <returns>A validated evaluation state envelope containing structural engine execution routing instructions.</returns>
    public async ValueTask<SymbolHandlingResult> InterceptSymbolsAsync(TerminalScreen screen, string userInput)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (userInput == "-h")
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ITerminalSessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ITerminalSessionRepository>();

            HelpScreen helpScreen = new()
            {
                Id = Guid.NewGuid(),
                Name = nameof(HelpScreen),
                ParentScreenId = screen.Id,
                SessionId = screen.SessionId
            };

            // Execute the infrastructure update completely asynchronously without thread blocking anti-patterns
            await sessionRepository.SaveActiveScreenAsync(screen.SessionId, helpScreen);

            return SymbolHandlingResult.RefreshActiveScreen();
        }

        return SymbolHandlingResult.NotHandled();
    }
}
