using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using Moq;
using Testcontainers.MongoDb;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.Persistence.Mongo.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.Persistence.Mongo.Tests.Repositories.Fakes;
using PixelTerminalUI.Persistence.Mongo.Repositories;
using PixelTerminalUI.StatelessEngine.Widgets;
using PixelTerminalUI.StatelessEngine.Screens;

namespace PixelTerminalUI.Persistence.Mongo.Tests.Repositories;

public sealed class MongoTerminalSessionRepositoryTests : IAsyncLifetime
{
    // The Testcontainers fixture that manages the real ephemeral Docker MongoDB instance lifecycle
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder("mongo:7.0")
        .Build();

    private IMongoDatabase _database = null!;
    private MongoTerminalSessionRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        IServiceCollection services = new ServiceCollection();
        services.AddTerminalMongoRepository(_mongoContainer.GetConnectionString(), "PixelTerminalTestDb");
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        _database = serviceProvider.GetRequiredService<IMongoDatabase>();

        RegisterClassMapsInfrastucture();

        _repository = new MongoTerminalSessionRepository(Mock.Of<ILogger<MongoTerminalSessionRepository>>(), _database);
    }

    public async Task DisposeAsync()
    {
        // Safely destroy and remove the Docker container after test completion to keep the machine clean
        await _mongoContainer.DisposeAsync();
    }

    private static void RegisterClassMapsInfrastucture()
    {
        // Idempotent registration wrapper for BaseScreen hierarchy
        if (!BsonClassMap.IsClassMapRegistered(typeof(TerminalScreen)))
        {
            BsonClassMap.RegisterClassMap<TerminalScreen>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(SimpleMessageScreen));
            });
        }

        // Idempotent registration wrapper for TextWidget hierarchy
        if (!BsonClassMap.IsClassMapRegistered(typeof(TextWidget)))
        {
            BsonClassMap.RegisterClassMap<TextWidget>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(TextEntryWidget));
            });
        }

        // Idempotent registration wrapper for polymorphic commands
        if (!BsonClassMap.IsClassMapRegistered(typeof(CommandBase)))
        {
            BsonClassMap.RegisterClassMap<CommandBase>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(CancelTasksCommand));
            });
        }
    }

    [Fact]
    public async Task SaveAndGetActiveScreenAsync_ShouldPreservePolymorphicTypesAndEmbeddedCommandStates()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid textEntryWidgetId = Guid.NewGuid();

        // Construct a highly complex polymorphic data structure replicating heavy production workloads
        CancelTasksCommand embeddedCommand = new()
        {
            State = CancelTasksState.AwaitingUserInput, // State is packed to int internally via Unsafe.As
            LastAttemptValue = "CONFIRM-CANCEL-99"
        };

        TextEntryWidget polymorphicWidget = new()
        {
            Id = textEntryWidgetId,
            Name = "CancellationReasonInput",
            Left = 0,
            Top = 2,
            Width = 15,
            Height = 1,
            Value = string.Empty,
            Visible = true,
            Command = embeddedCommand // Attaching the polymorphic command base reference
        };

        SimpleMessageScreen scrollScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "TaskCancellationView",
            Visible = true,
            Width = 40,
            Height = 12,
            FocusedEntryWidgetId = textEntryWidgetId,
            Widgets = [polymorphicWidget]
        };

        // Act
        // Persist the nested structural entity graph into the real running MongoDB document database
        await _repository.SaveActiveScreenAsync(sessionId, scrollScreen);

        // Assert
        // Verify screen polymorphic deserialization matching exact type definitions
        TerminalScreen? retrievedScreenBase = await _repository.GetActiveScreenAsync(sessionId);
        retrievedScreenBase
            .Should()
            .NotBeNull()
            .And.BeOfType<SimpleMessageScreen>("because the framework must reconstruct the original concrete screen type layout");

        // Verify widgets polymorphic extraction from embedded collection hierarchy
        SimpleMessageScreen deserializedScrollScreen = (SimpleMessageScreen)retrievedScreenBase!;
        deserializedScrollScreen.Widgets.Should().HaveCount(1);
        TextWidget retrievedWidgetBase = deserializedScrollScreen.Widgets.First(); // Safe collection item access
        retrievedWidgetBase.Should().BeOfType<TextEntryWidget>();

        // Verify Command mapping, original runtime type binding and byte packing execution stability
        TextEntryWidget deserializedEntryWidget = (TextEntryWidget)retrievedWidgetBase;
        deserializedEntryWidget.Command
            .Should()
            .NotBeNull("because polymorphic MongoDB mapping should correctly bind embedded nested commands objects")
            .And.BeOfType<CancelTasksCommand>("because MongoDB type discriminators must resolve concrete plugin command entities");

        CancelTasksCommand deserializedCommand = (CancelTasksCommand)deserializedEntryWidget.Command!;
        deserializedCommand.State
            .Should()
            .Be(CancelTasksState.AwaitingUserInput, "the internal Unsafe.As property bitwise packing structure must correctly materialize back from raw integer db field values");
        deserializedCommand.LastAttemptValue
            .Should()
            .Be("CONFIRM-CANCEL-99");
    }

    [Fact]
    public async Task SaveActiveScreenAsync_WhenEmbeddedCommandStateMutates_ShouldAtomicallyUpdateRawStateInMongo()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid widgetId = Guid.NewGuid();

        CancelTasksCommand command = new() { WidgetId = widgetId, State = CancelTasksState.Undefined };
        TextEntryWidget widget = new() { Id = widgetId, Name = "TextEdit", Value = string.Empty, Visible = true, Command = command };
        SimpleMessageScreen screen = new() { Id = Guid.NewGuid(), Name = "SimpleMessage", SessionId = sessionId, Widgets = [widget] };

        await _repository.SaveActiveScreenAsync(sessionId, screen);

        // Act: Mutate the tracking command workflow state step to emulate step execution
        command.State = CancelTasksState.AwaitingUserInput; // Packed to '1' bitwise under the hood
        await _repository.SaveActiveScreenAsync(sessionId, screen); // Save mutation graph

        // Pull directly out of the database wire container instance
        TerminalScreen? retrievedScreen = await _repository.GetActiveScreenAsync(sessionId);
        SimpleMessageScreen typedScreen = (SimpleMessageScreen)retrievedScreen!;
        TextEntryWidget typedWidget = (TextEntryWidget)typedScreen.Widgets.First();
        CancelTasksCommand retrievedCommand = (CancelTasksCommand)typedWidget.Command!;

        // Assert
        retrievedCommand.State
            .Should()
            .Be(CancelTasksState.AwaitingUserInput, "because the framework data access components must cleanly synchronize nested mutational graph payloads");
    }

    [Fact]
    public async Task GetActiveScreenAsync_ShouldMaintainStrictIsolationBetweenDistinctUserSessionIds()
    {
        // Arrange
        Guid sessionUserA = Guid.NewGuid();
        Guid sessionUserB = Guid.NewGuid();
        Guid widgetId = Guid.NewGuid();

        // User A opens the stock screen and types item data
        TextEntryWidget widgetA = new() { Id = widgetId, Name = "WidgetA", Value = "PALLET-AAA", Visible = true };
        SimpleMessageScreen screenA = new() { Id = Guid.NewGuid(), SessionId = sessionUserA, Name = "StockScreen", Widgets = [widgetA] };

        // User B opens the EXACT same stock screen schema view but types different inventory data
        TextEntryWidget widgetB = new() { Id = widgetId, Name = "WidgetB", Value = "PALLET-BBB", Visible = true };
        SimpleMessageScreen screenB = new() { Id = Guid.NewGuid(), SessionId = sessionUserB, Name = "StockScreen", Widgets = [widgetB] };

        // Act: Commit both parallel pipeline entities to the shared underlying collection storage
        await _repository.SaveActiveScreenAsync(sessionUserA, screenA);
        await _repository.SaveActiveScreenAsync(sessionUserB, screenB);

        // Fetch back datasets independently using distinct correlation keys
        TerminalScreen? fetchedScreenA = await _repository.GetActiveScreenAsync(sessionUserA);
        TerminalScreen? fetchedScreenB = await _repository.GetActiveScreenAsync(sessionUserB);

        TextEntryWidget finalWidgetA = (TextEntryWidget)fetchedScreenA!.Widgets.First();
        TextEntryWidget finalWidgetB = (TextEntryWidget)fetchedScreenB!.Widgets.First();

        // Assert
        finalWidgetA.Value
            .Should()
            .Be("PALLET-AAA", "because User A session container metrics must remain completely isolated from concurrent mutations");

        finalWidgetB.Value
            .Should()
            .Be("PALLET-BBB", "because User B session container metrics must remain completely isolated from concurrent mutations")
            .And
            .NotBe(finalWidgetA.Value, "because shared runtime structures must map to separate underlying MongoDB documents entries");
    }
}
