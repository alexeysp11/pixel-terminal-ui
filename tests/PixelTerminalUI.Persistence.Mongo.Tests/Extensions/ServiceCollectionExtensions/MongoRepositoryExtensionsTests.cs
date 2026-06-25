using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PixelTerminalUI.Persistence.Mongo.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.Persistence.Mongo.Tests.Extensions.ServiceCollectionExtensions.Fakes;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.Persistence.Mongo.Tests.Extensions.ServiceCollectionExtensions;

public sealed class MongoRepositoryExtensionsTests
{
    private const string TargetConnectionString = "mongodb://localhost:27017";
    private const string TargetDatabaseName = "TestSessions";

    [Fact]
    public void AddMongoUserSessionRepository_WhenInvoked_ShouldRegisterRequiredAbstractions()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);
        services.AddLogging();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IMongoClient? mongoClient = serviceProvider.GetService<IMongoClient>();
        IMongoDatabase? mongoDatabase = serviceProvider.GetService<IMongoDatabase>();
        ITerminalSessionRepository? repository = serviceProvider.GetService<ITerminalSessionRepository>();

        mongoClient
            .Should()
            .NotBeNull("because the primary MongoDB native client abstraction must be exposed to the application host");

        mongoDatabase
            .Should()
            .NotBeNull("because the database reference token must be injected for repository dependency resolution");

        repository
            .Should()
            .NotBeNull("because the concrete user session persistence layer must procreate within the request handling pipeline");
    }

    [Fact]
    public void AddMongoUserSessionRepository_ShouldRegisterMongoClientAsSingletonAndDatabaseAsScoped()
    {
        // Arrange
        IServiceCollection? services = new ServiceCollection();
        services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);
        ServiceProvider? serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        // Verify IMongoClient has a Singleton lifestyle (reused across the entire application lifetime)
        IMongoClient? clientInstance1 = serviceProvider.GetRequiredService<IMongoClient>();
        IMongoClient? clientInstance2 = serviceProvider.GetRequiredService<IMongoClient>();

        clientInstance1
            .Should()
            .BeSameAs(clientInstance2, "because IMongoClient maintains an internal connection pool and must be a Singleton");

        // 2. Verify IMongoDatabase has a Scoped lifestyle (isolated per HTTP request session loop)
        using (IServiceScope scope1 = serviceProvider.CreateScope())
        using (IServiceScope scope2 = serviceProvider.CreateScope())
        {
            IMongoDatabase? dbFromScope1 = scope1.ServiceProvider.GetRequiredService<IMongoDatabase>();
            IMongoDatabase? dbFromScope1Repeat = scope1.ServiceProvider.GetRequiredService<IMongoDatabase>();
            IMongoDatabase? dbFromScope2 = scope2.ServiceProvider.GetRequiredService<IMongoDatabase>();

            dbFromScope1
                .Should()
                .BeSameAs(dbFromScope1Repeat, "because IMongoDatabase should resolve to the same instance within a single scope lifecycle execution")
                .And
                .NotBeSameAs(dbFromScope2, "because different execution scopes must yield isolated database instances reference pointers");
        }
    }

    [Fact]
    public void AddMongoUserSessionRepository_ShouldRegisterRepositoryWithScopedLifestyle()
    {
        // Arrange
        IServiceCollection? services = new ServiceCollection();
        services.AddLogging();
        services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        using (IServiceScope scope1 = serviceProvider.CreateScope())
        using (IServiceScope scope2 = serviceProvider.CreateScope())
        {
            ITerminalSessionRepository repoFromScope1 = scope1.ServiceProvider.GetRequiredService<ITerminalSessionRepository>();
            ITerminalSessionRepository repoFromScope1Repeat = scope1.ServiceProvider.GetRequiredService<ITerminalSessionRepository>();
            ITerminalSessionRepository repoFromScope2 = scope2.ServiceProvider.GetRequiredService<ITerminalSessionRepository>();

            repoFromScope1
                .Should()
                .BeSameAs(repoFromScope1Repeat, "because the user repository must resolve to the same state inside a single tracking transaction context")
                .And
                .NotBeSameAs(repoFromScope2, "because database mutation operations must be strictly isolated between distinct concurrent processing pipelines");
        }
    }

    [Fact]
    public void AddMongoUserSessionRepository_ShouldPassCorrectDatabaseNameParameterToMongoDriver()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        IMongoDatabase mongoDatabase = serviceProvider.GetRequiredService<IMongoDatabase>();

        // Assert
        mongoDatabase.DatabaseNamespace.DatabaseName
            .Should()
            .Be(TargetDatabaseName, "I expect that the exact naming convention string supplied via client configuration maps directly into driver internals");
    }

    [Fact]
    public void AddMongoUserSessionRepository_CanBeSafelyCalledMultipleTimes_WithoutThrowingBsonSerializationException()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        // We simulate a race condition where multiple registration extensions or duplicate scripts fire up sequentially
        Action multipleRegistrationsAction = () =>
        {
            services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);
            services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);
        };

        // Assert
        multipleRegistrationsAction
            .Should()
            .NotThrow("because the underlying registration logic protects global GuidSerializer setup from throwing duplication exceptions using Interlocked guard");
    }

    [Fact]
    public void AddMongoUserSessionRepository_UnderHeavyThreadContention_ShouldConsistentlyRegisterInfrastructureCorrectly()
    {
        // Arrange
        int threadCount = 8;
        Task[] tasks = new Task[threadCount];

        // We create an array of service collections to isolate DI mutations per concurrent thread,
        // ensuring we only stress-test the global static BsonSerializer Interlocked exchange primitive.
        IServiceCollection[] serviceCollections = new IServiceCollection[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            serviceCollections[i] = new ServiceCollection();
        }

        // Act
        // We intentionally execute massive parallel multi-threaded invocations to hammer the global driver configuration state
        for (int i = 0; i < threadCount; i++)
        {
            int localIndex = i; // Avoid closure variable capturing pitfalls
            tasks[localIndex] = Task.Run(() =>
            {
                serviceCollections[localIndex].AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);
                serviceCollections[localIndex].AddLogging();
            });
        }

        Action parallelExecutionAction = () => Task.WaitAll(tasks);

        // Assert
        parallelExecutionAction
            .Should()
            .NotThrow("because atomic thread-safe primitives completely eliminate runtime data races when configuring database driver metadata layers");

        // Verify that every single isolated container successfully built its dependencies maps without corrupted structures
        for (int i = 0; i < threadCount; i++)
        {
            ServiceProvider serviceProvider = serviceCollections[i].BuildServiceProvider();
            ITerminalSessionRepository? repository = serviceProvider.GetService<ITerminalSessionRepository>();

            repository
                .Should()
                .NotBeNull("because the container schema structure remains flawless inside every concurrent worker loop thread execution");
        }
    }

    [Fact]
    public void AddMongoUserSessionRepository_ShouldRegisterSystemLevelPolymorphicBsonClassMapsByDefault()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName);

        // Assert
        BsonClassMap.IsClassMapRegistered(typeof(TerminalScreen))
            .Should()
            .BeTrue("because the core framework screen model root contract must register ahead of time to support polymorphic tracking");

        BsonClassMap.IsClassMapRegistered(typeof(SimpleMessageScreen))
            .Should()
            .BeTrue("because systemic generic notification dialog views need built-in blueprint mappings inside the storage layer");

        BsonClassMap.IsClassMapRegistered(typeof(TextWidget))
            .Should()
            .BeTrue("because the foundation abstract GUI component entity contract must bind its layout scheme definition explicitly");

        BsonClassMap.IsClassMapRegistered(typeof(TextEntryWidget))
            .Should()
            .BeTrue("because interactive user input text processing fields require a system-level document mapping serialization strategy");

        BsonClassMap.IsClassMapRegistered(typeof(CommandBase))
            .Should()
            .BeTrue("because stateless business logic trigger handlers require a shared polymorphic deserialization intercept marker");
    }

    [Fact]
    public void AddMongoUserSessionRepository_ShouldInvokeCustomScreenInitializerDelegate_ToMountClientDomainTypes()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddTerminalMongoRepository(TargetConnectionString, TargetDatabaseName, initializer => initializer
            .RegisterScreen<CustomDummyScreen>()
            .RegisterWidget<CustomDummyWidget>()
            .RegisterCommand<CustomDummyCommand>());

        // Assert
        BsonClassMap.IsClassMapRegistered(typeof(CustomDummyScreen))
            .Should()
            .BeTrue("because game-specific layout interfaces need explicit registration to avoid runtime concrete model instance mapping failures");

        BsonClassMap.IsClassMapRegistered(typeof(CustomDummyWidget))
            .Should()
            .BeTrue("because custom visual terminal component widgets must declare themselves as valid polymorphic elements within widget tree array collections");

        BsonClassMap.IsClassMapRegistered(typeof(CustomDummyCommand))
            .Should()
            .BeTrue("because independent state-machine commands require dedicated serialization registration paths to properly capture active state indices inside MongoDB");
    }
}
