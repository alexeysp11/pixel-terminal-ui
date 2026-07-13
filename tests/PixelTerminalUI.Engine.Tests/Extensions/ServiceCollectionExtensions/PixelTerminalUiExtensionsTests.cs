using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PixelTerminalUI.Engine.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.Engine.Factories.TerminalErrorScreen;
using PixelTerminalUI.Engine.Rendering.Core;
using PixelTerminalUI.Engine.Rendering.Registries;
using PixelTerminalUI.Engine.Rendering.WidgetRendering;
using PixelTerminalUI.Engine.Tests.Extensions.ServiceCollectionExtensions.Fakes;

namespace PixelTerminalUI.Engine.Tests.Extensions.ServiceCollectionExtensions;

public sealed class PixelTerminalUiExtensionsTests
{
    [Fact]
    public void AddPixelTerminalUI_WhenInvokedWithoutOptions_ShouldRegisterCoreServicesAndDefaultOptions()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddPixelTerminalUI();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IStatelessRenderer? renderer = serviceProvider.GetService<IStatelessRenderer>();
        ITerminalErrorScreenFactory? errorScreenFactory = serviceProvider.GetService<ITerminalErrorScreenFactory>();
        IWidgetRendererRegistry? registry = serviceProvider.GetService<IWidgetRendererRegistry>();
        PixelTerminalUIOptions? options = serviceProvider.GetService<PixelTerminalUIOptions>();

        renderer
            .Should()
            .NotBeNull("because the core stateless rendering engine must be registered via its interface abstraction");

        errorScreenFactory
            .Should()
            .NotBeNull();

        registry
            .Should()
            .NotBeNull("because the system requires a central lookup registry for widget renderers via its interface abstraction");

        options
            .Should()
            .NotBeNull("because the framework must automatically provide a default options configuration instance when none is passed")
            .And.Match<PixelTerminalUIOptions>(o => o.EnableDoubleBuffering == true, "because double buffering must be enabled by default for optimal network bandwidth usage");

        // Verify Singleton lifetime behavior by resolving twice and checking reference equality
        IStatelessRenderer rendererSecondInstance = serviceProvider.GetRequiredService<IStatelessRenderer>();
        rendererSecondInstance
            .Should()
            .BeSameAs(renderer, "because the core stateless rendering engine must follow a Singleton lifetime pattern");

        IWidgetRendererRegistry registrySecondInstance = serviceProvider.GetRequiredService<IWidgetRendererRegistry>();
        registrySecondInstance
            .Should()
            .BeSameAs(registry, "because the widget renderer registry must follow a Singleton lifetime pattern");
    }

    [Fact]
    public void AddPixelTerminalUI_WhenInvokedWithCustomOptions_ShouldApplyAndRegisterThemClean()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddPixelTerminalUI(options =>
        {
            options.EnableDoubleBuffering = false;
        });
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        PixelTerminalUIOptions? options = serviceProvider.GetService<PixelTerminalUIOptions>();

        // Assert
        options
            .Should()
            .NotBeNull("because explicit user-defined configuration lambda blocks must still yield a valid options record container")
            .And.Match<PixelTerminalUIOptions>(o => o.EnableDoubleBuffering == false, "because the custom options values provided by the host client application must be properly materialized");
    }

    [Fact]
    public void AddPixelTerminalUI_WhenInvoked_ShouldRegisterAllStandardBoxRenderers()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddPixelTerminalUI();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        IEnumerable<IWidgetRenderer> registeredRenderers = serviceProvider.GetServices<IWidgetRenderer>();
        List<Type> rendererTypes = registeredRenderers.Select(r => r.GetType()).ToList();

        // Assert
        rendererTypes
            .Should()
            .HaveCount(3, "because the framework provides exactly three standard built-in widget renderers out of the box");

        rendererTypes
            .Should()
            .Contain([
                typeof(TextWidgetRenderer),
                typeof(TextEntryWidgetRenderer),
                typeof(PasswordEntryWidgetRenderer)
            ], "because all standard text input and presentation widgets must be available immediately within the core application package");
    }

    [Fact]
    public void AddCustomTerminalRenderer_WhenInvokedByPlugin_ShouldAppendCustomRendererToCollection()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        services.AddPixelTerminalUI();

        // Act
        services.AddCustomTerminalRenderer<CustomDummyWidget, CustomDummyWidgetRenderer>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        IEnumerable<IWidgetRenderer> registeredRenderers = serviceProvider.GetServices<IWidgetRenderer>();
        List<Type> rendererTypes = registeredRenderers.Select(r => r.GetType()).ToList();

        // Assert
        rendererTypes
            .Should()
            .HaveCount(4, "because the custom plugin renderer should be appended onto the existing built-in renderers collection layout");

        rendererTypes
            .Should()
            .Contain(typeof(CustomDummyWidgetRenderer), "because the client application registered a custom plugin UI extension component");
    }

    [Fact]
    public void AddCustomTerminalRenderer_CanBeChained_WhenConfiguringMultiplePluginExtensions()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services
            .AddPixelTerminalUI()
            .AddCustomTerminalRenderer<CustomDummyWidget, CustomDummyWidgetRenderer>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IWidgetRenderer? customRenderer = serviceProvider.GetServices<IWidgetRenderer>()
            .FirstOrDefault(r => r.GetType() == typeof(CustomDummyWidgetRenderer));

        // Assert
        customRenderer
            .Should()
            .NotBeNull("because fluent chaining must properly carry over the underlying IServiceCollection context references");
    }

    [Fact]
    public void AddCustomTerminalRenderer_WhenMultipleSameRenderersRegistered_ShouldAllowDuplicatesInCollection()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        services.AddPixelTerminalUI();

        // Act
        services.AddCustomTerminalRenderer<CustomDummyWidget, CustomDummyWidgetRenderer>();
        services.AddCustomTerminalRenderer<CustomDummyWidget, CustomDummyWidgetRenderer>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IEnumerable<IWidgetRenderer> registeredRenderers = serviceProvider.GetServices<IWidgetRenderer>();
        List<IWidgetRenderer> pluginInstances = registeredRenderers.Where(r => r.GetType() == typeof(CustomDummyWidgetRenderer)).ToList();

        // Assert
        pluginInstances
            .Should()
            .HaveCount(2, "because native dependency injection service descriptors allow multiple registration instances of the same service contract type");
    }
}
