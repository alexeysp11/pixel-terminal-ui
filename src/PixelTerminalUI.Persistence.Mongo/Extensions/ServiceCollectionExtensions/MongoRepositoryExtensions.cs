using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.Persistence.Mongo.Repositories;
using PixelTerminalUI.StatelessEngine.Widgets;
using PixelTerminalUI.StatelessEngine.Screens;

namespace PixelTerminalUI.Persistence.Mongo.Extensions.ServiceCollectionExtensions;

public static class MongoRepositoryExtensions
{
    private static int _mongoSerializationRegistrationState = 0;

    /// <summary>
    /// Extension method for the client application to seamlessly configure the MongoDB storage layer.
    /// </summary>
    public static IServiceCollection AddTerminalMongoRepository(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Action<BsonClassMapInitializer>? configureCustomScreens = null)
    {
        if (Interlocked.Exchange(ref _mongoSerializationRegistrationState, 1) == 0)
        {
            // Standardize systemic infrastructure primitive representation mappings
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // Register system-level base screens polymorphic architecture framework roots
            BsonClassMap.RegisterClassMap<TerminalScreen>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });
            BsonClassMap.RegisterClassMap<SimpleMessageScreen>();

            // Register system-level widgets polymorphic architecture hierarchy
            BsonClassMap.RegisterClassMap<TextWidget>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });
            BsonClassMap.RegisterClassMap<PasswordEntryWidget>();
            BsonClassMap.RegisterClassMap<TextEntryWidget>();

            // Register system-level commands polymorphic hierarchy
            BsonClassMap.RegisterClassMap<CommandBase>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });
        }

        // Invoke the external customization delegate callback to dynamically mount client application layout schemes
        if (configureCustomScreens != null)
        {
            BsonClassMapInitializer initializer = new();
            configureCustomScreens(initializer);
        }

        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddScoped<ITerminalSessionRepository, MongoTerminalSessionRepository>();

        return services;
    }
}
