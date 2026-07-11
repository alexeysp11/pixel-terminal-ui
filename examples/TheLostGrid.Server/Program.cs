using Microsoft.AspNetCore.Server.Kestrel.Core;
using PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.StatelessEngine.Commands.DismissError;
using PixelTerminalUI.StatelessEngine.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.StatelessEngine.SymbolHandling;
using PixelTerminalUI.StatelessEngine.Validators;
using PixelTerminalUI.Transport.Grpc;
using ProtoBuf.Grpc.Server;
using Serilog;
using TheLostGrid.Server.Infrastructure.Interceptors;
using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using TheLostGrid.Server.Scenarios.Help;
using TheLostGrid.Server.Scenarios.PowerGridTerminal;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;
using TheLostGrid.Server.Scenarios.Welcome;
using TheLostGrid.Server.Services;

namespace TheLostGrid.Server;

/// <summary>
/// Provides the definitive entry point to bootstrap the pure gRPC application architecture.
/// </summary>
public sealed class Program
{
    /// <summary>
    /// Confirms service registration schemas and maps the target immutable gRPC service endpoints.
    /// </summary>
    /// <param name="args">The explicit command-line arguments collection matrix passed during startup.</param>
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Bootstrap logging layers immediately to track container structural allocation phases
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // Enforce Kestrel endpoints behavior to handle pure high performance HTTP/2 protocol frames without TLS
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureEndpointDefaults(listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        // Core PixelTerminalUI engine pipeline bootstrap
        builder.Services.AddPixelTerminalUI(options =>
        {
            options.EnableDoubleBuffering = true;
        });
        builder.Services.AddPixelTerminalStartup<WelcomeScreen>();

        // Resolve connection string from configuration layer to enable seamless Docker mapping
        string redisConnectionString = builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Unable to get Redis connection string");

        // Attach optimized Redis state delivery distribution repository layer
        builder.Services
            .AddTerminalRedisRepository(redisConnectionString)
            .WithSessionTimeout(TimeSpan.FromMinutes(30))
            .RegisterCustomScreens(custom => custom
                // Screens registration
                .RegisterScreen<HelpScreen>()
                .RegisterScreen<WelcomeScreen>()
                .RegisterScreen<CharacterCreationScreen>()
                .RegisterScreen<SectorNavigationScreen>()
                .RegisterScreen<TerminalHackScreen>()
                .RegisterScreen<SectorScannerScreen>()
                .RegisterScreen<DroneDeploymentScreen>()
                .RegisterScreen<PowerGridTerminalScreen>()

                // Commands registration
                .RegisterCommand<HelpDismissCommand>()
                .RegisterCommand<WelcomeStartGameCommand>()
                .RegisterCommand<CharacterCreationSubmitCommand>()
                .RegisterCommand<SectorNavigationExploreCommand>()
                .RegisterCommand<TerminalHackSubmitKeyCommand>()
                .RegisterCommand<SectorScannerScanCommand>()
                .RegisterCommand<DismissErrorCommand>()
                .RegisterCommand<DroneDeploymentDeployCommand>()
                .RegisterCommand<PowerGridTerminalBuyEnergyCommand>());

        // Attach layout level presentation validation constraints routines
        builder.Services.AddScreenValidators(options =>
        {
            options.ForScreen(nameof(CharacterCreationScreen))
                   .Add((screen, input) => input.Length > 15
                       ? ValidationResult.Fail("OVERFLOW: Max 15 characters allowed!")
                       : ValidationResult.Success());
        });

        // Intercept and configure the singleton special symbol handler with game-specific macro routines
        builder.Services.AddSingleton<ISpecialSymbolHandler>(provider =>
        {
            IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            GameplayInputInterceptor interceptor = new(scopeFactory);
            SpecialSymbolHandler handler = new()
            {
                CustomInterceptor = interceptor.InterceptSymbolsAsync
            };
            return handler;
        });

        // Register high performance code-first gRPC services pipelines
        builder.Services.AddCodeFirstGrpc();

        WebApplication app = builder.Build();

        // Compile binary contract layouts before binding physical HTTP/2 pipeline tracks
        GrpcModelConfiguration.RegisterTerminalContracts();

        // Explicitly map the standalone gRPC service route directly without reflection engines
        app.MapGrpcService<TerminalGrpcService>();

        app.Run();
    }
}