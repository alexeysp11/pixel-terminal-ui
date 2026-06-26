using TheLostGrid.Server.Extensions;
using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;
using TheLostGrid.Server.Scenarios.Welcome;
using Serilog;
using PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.StatelessEngine.Commands.DismissError;
using PixelTerminalUI.StatelessEngine.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.StatelessEngine.Validators;

namespace TheLostGrid.Server;

public sealed class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Composition Root: Infrastructure service collection registrations setup
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddLogging();

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
        builder.Services.AddTerminalRedisRepository(
            connectionString: redisConnectionString,
            configureCustomScreens: custom => custom
                // Screens registration
                .RegisterScreen<WelcomeScreen>()
                .RegisterScreen<CharacterCreationScreen>()
                .RegisterScreen<SectorNavigationScreen>()
                .RegisterScreen<TerminalHackScreen>()
                .RegisterScreen<SectorScannerScreen>()
                .RegisterScreen<DroneDeploymentScreen>()

                // Commands registration
                .RegisterCommand<ConnectNeuralLinkCommand>()
                .RegisterCommand<RegisterOperatorCommand>()
                .RegisterCommand<ExploreSectorCommand>()
                .RegisterCommand<SubmitHackKeyCommand>()
                .RegisterCommand<ScanSectorsCommand>()
                .RegisterCommand<DismissErrorCommand>()
                .RegisterCommand<DeployDroneCommand>());

        // Attach layout level presentation validation constraints routines
        builder.Services.AddScreenValidators(options =>
        {
            options.ForScreen(nameof(CharacterCreationScreen))
                   .Add((screen, input) => input.Length > 10
                       ? ValidationResult.Fail("Input exceeds limit!")
                       : ValidationResult.Success());

            options.ForScreen(nameof(WelcomeScreen))
                   .Add((screen, input) => input == "-m"
                       ? ValidationResult.Fail("Menu is not available yet!")
                       : ValidationResult.Success());
        });

        builder.Services.AddModuleEndpoints();

        // Serilog.
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        WebApplication app = builder.Build();

        // HTTP Processing Pipeline pipeline configuration mapping routines execution
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Wire up the scanned endpoints routing blocks automatically without polluting this file code structure
        app.MapModuleEndpoints();

        app.Run();
    }
}
