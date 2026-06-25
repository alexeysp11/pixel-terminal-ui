using MongoDB.Bson.Serialization;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.Persistence.Mongo.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Fluent helper infrastructure contract container to safely encapsulate class map registration actions boundary layers.
/// </summary>
public sealed class BsonClassMapInitializer
{
    public BsonClassMapInitializer RegisterScreen<TScreen>() where TScreen : TerminalScreen
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TScreen)))
        {
            BsonClassMap.RegisterClassMap<TScreen>();
        }
        return this;
    }

    public BsonClassMapInitializer RegisterCommand<TCommand>() where TCommand : CommandBase
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TCommand)))
        {
            BsonClassMap.RegisterClassMap<TCommand>();
        }
        return this;
    }

    /// <summary>
    /// Dynamic mounting method to cleanly map game specific custom TUI UI widgets hierarchy layout components
    /// </summary>
    /// <typeparam name="TWidget"></typeparam>
    /// <returns></returns>
    public BsonClassMapInitializer RegisterWidget<TWidget>() where TWidget : TextWidget
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(TWidget)))
        {
            BsonClassMap.RegisterClassMap<TWidget>();
        }
        return this;
    }
}
