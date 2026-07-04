using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixelTerminalUI.StatelessEngine.Factories.StartupScreen;
using PixelTerminalUI.StatelessEngine.Factories.TerminalErrorScreen;
using PixelTerminalUI.StatelessEngine.Navigation;
using PixelTerminalUI.StatelessEngine.Rendering.Core;
using PixelTerminalUI.StatelessEngine.Rendering.Registries;
using PixelTerminalUI.StatelessEngine.Rendering.WidgetRendering;
using PixelTerminalUI.StatelessEngine.RequestPipeline;
using PixelTerminalUI.StatelessEngine.ResponseBuilders;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.SymbolHandling;
using PixelTerminalUI.StatelessEngine.Validators.ValidationProviders;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Extensions.ServiceCollectionExtensions;

public static class PixelTerminalUiExtensions
{
    /// <summary>
    /// Basic engine initialization.
    /// </summary>
    public static IServiceCollection AddPixelTerminalUI(this IServiceCollection services, Action<PixelTerminalUIOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Initialize and execute configuration options if provided by the client application
        PixelTerminalUIOptions options = new();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddScoped<IFocusManager, FocusManager>();
        services.AddScoped<ISpecialSymbolHandler, SpecialSymbolHandler>();
        services.AddScoped<IRequestPipelineHandler, RequestPipelineHandler>();

        // Register the registry using its explicit interface abstraction
        services.AddSingleton<IWidgetRendererRegistry, WidgetRendererRegistry>();

        // Registering the rendering system core
        services.AddSingleton<IStatelessRenderer, StatelessRenderer>();
        services.AddSingleton<ITerminalErrorScreenFactory, TerminalErrorScreenFactory>();

        // Register standard renderers that come "out of the box"
        services.AddSingleton<IWidgetRenderer, TextWidgetRenderer>();
        services.AddSingleton<IWidgetRenderer, TextEntryWidgetRenderer>();
        services.AddSingleton<IWidgetRenderer, PasswordEntryWidgetRenderer>();

        // Register response builders
        services.AddSingleton<IAdaptiveResponseBuilder, AdaptiveResponseBuilder>();

        return services;
    }

    /// <summary>
    /// Method for extending the system with new widgets.
    /// </summary>
    public static IServiceCollection AddCustomTerminalRenderer<TWidget, TRenderer>(this IServiceCollection services)
        where TWidget : TextWidget
        where TRenderer : class, IWidgetRenderer
    {
        // Simply register the custom renderer as another IWidgetRenderer.
        // Service descriptors in .NET allow you to add multiple implementations of a single interface.
        services.AddSingleton<IWidgetRenderer, TRenderer>();

        return services;
    }

    /// <summary>
    /// Registers the specific concrete screen type that the framework will instantiate 
    /// whenever a new user session performs a Cold Start sequence.
    /// </summary>
    public static IServiceCollection AddPixelTerminalStartup<TScreen>(this IServiceCollection services)
        where TScreen : TerminalScreen
    {
        // Register the concrete screen type inside the DI container container as Transient dependency
        services.AddTransient<TScreen>();

        // Inject runtime context providers factory referencing the specific type metadata
        services.AddSingleton<IStartupScreenFactory>(sp =>
            new StartupScreenFactory(
                sp.GetRequiredService<ILogger<StartupScreenFactory>>(),
                typeof(TScreen),
                type => (TerminalScreen)sp.GetRequiredService(type)));

        return services;
    }

    /// <summary>
    /// Registers the specific concrete screen type using reflection metadata definitions.
    /// </summary>
    public static IServiceCollection AddPixelTerminalStartup(this IServiceCollection services, Type screenType)
    {
        ArgumentNullException.ThrowIfNull(screenType);

        if (!typeof(TerminalScreen).IsAssignableFrom(screenType))
        {
            throw new ArgumentException($"The specified type {screenType.Name} must derive from {nameof(TerminalScreen)}", nameof(screenType));
        }

        // Register type dynamically within DI services descriptor registry maps
        services.AddTransient(screenType);

        services.AddSingleton<IStartupScreenFactory>(sp =>
            new StartupScreenFactory(
                sp.GetRequiredService<ILogger<StartupScreenFactory>>(),
                screenType,
                type => (TerminalScreen)sp.GetRequiredService(type)));

        return services;
    }

    /// <summary>
    /// Registers the central screen validation infrastructure inside the host IoC dependency injection container using an immutable configuration delegate pipeline.
    /// </summary>
    /// <param name="services">The centralized system service collection instance tracking runtime dependencies definitions mapping.</param>
    /// <param name="configure">The configuration delegate block used to populate target interface screen validation rules indexes.</param>
    /// <returns>The primary service collection instance to maintain uninterrupted dependency chain setup paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided service collection or configuration delegate block evaluates to null.</exception>
    public static IServiceCollection AddScreenValidators(this IServiceCollection services, Action<ScreenValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        ScreenValidationOptions options = new();
        configure(options);

        ScreenValidationProvider provider = new(options.BuildRegistry());
        services.AddSingleton<IScreenValidationProvider>(provider);

        return services;
    }
}
