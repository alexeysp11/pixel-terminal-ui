using MongoDB.Bson.Serialization;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.Persistence.Mongo.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Provides mechanism to explicitly register polymorphic domain models within MongoDB class maps mappings.
/// </summary>
public sealed class BsonClassMapInitializer
{
    /// <summary>
    /// Registers a concrete terminal screen type into the Bson class mapper context.
    /// </summary>
    /// <typeparam name="TScreen">The type of the custom terminal screen implementation derived from base terminal screen class.</typeparam>
    /// <returns>The current instance of the initializer to enable fluent invocation chains.</returns>
    public BsonClassMapInitializer RegisterScreen<TScreen>() where TScreen : TerminalScreen
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TScreen)))
        {
            BsonClassMap.RegisterClassMap<TScreen>();
        }
        return this;
    }

    /// <summary>
    /// Registers a concrete command payload type into the Bson class mapper context.
    /// </summary>
    /// <typeparam name="TCommand">The type of the custom runtime execution command entity derived from base command abstraction.</typeparam>
    /// <returns>The current instance of the initializer to enable fluent invocation chains.</returns>
    public BsonClassMapInitializer RegisterCommand<TCommand>() where TCommand : CommandBase
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TCommand)))
        {
            BsonClassMap.RegisterClassMap<TCommand>();
        }
        return this;
    }

    /// <summary>
    /// Registers a concrete layout rendering visual widget type into the Bson class mapper context.
    /// </summary>
    /// <typeparam name="TWidget">The type of the target presentation component entity derived from base widget interface block.</typeparam>
    /// <returns>The current instance of the initializer to enable fluent invocation chains.</returns>
    public BsonClassMapInitializer RegisterWidget<TWidget>() where TWidget : TextWidget
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TWidget)))
        {
            BsonClassMap.RegisterClassMap<TWidget>();
        }
        return this;
    }
}
