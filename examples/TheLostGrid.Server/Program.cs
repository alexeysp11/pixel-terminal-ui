using TheLostGrid.Server.Extensions;
using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;
using TheLostGrid.Server.Scenarios.Welcome;
using PixelTerminalUI.StatelessEngine.Commands.DismissError;
using PixelTerminalUI.StatelessEngine.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.Persistence.Mongo.Extensions.ServiceCollectionExtensions;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using PixelTerminalUI.StatelessEngine.Validators;

namespace TheLostGrid.Server;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        //Log.Logger = new LoggerConfiguration()
        //    .WriteTo.Console()
        //    .WriteTo.PostgreSQL(
        //        connectionString: "Host=localhost;Database=AuditLogsDb;Username=postgres;Password=secret",
        //        tableName: "audit_events",
        //        needAutoCreateTable: true)
        //    .CreateLogger();

        // Composition Root: Infrastructure service collection registrations setup
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddLogging();

        builder.Services.AddPixelTerminalUI(options =>
        {
            options.EnableDoubleBuffering = true;
        });
        builder.Services.AddPixelTerminalStartup<WelcomeScreen>();
        builder.Services.AddTerminalMongoRepository(
            "mongodb://admin:secret_password_123@localhost:27017/?authSource=admin",
            "TheLostGridGameDb",
            custom => custom
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

        WebApplication app = builder.Build();

        // HTTP Processing Pipeline pipeline configuration mapping routines execution
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Wire up the scanned endpoints routing blocks automatically without polluting this file code structure
        app.MapModuleEndpoints();

        app.Run();
    }
}
