using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using PixelTerminalUI.StatelessEngine.Commands.Core;

namespace PixelTerminalUI.Persistence.Redis.Configuration;

/// <summary>
/// Orchestrates dynamic contract metadata modifications to inject polymorphic JSON type discriminators into stateless domain models execution boundaries.
/// </summary>
public sealed class RedisJsonTypeResolver
{
    private readonly List<Type> _screens = [];
    private readonly List<Type> _commands = [];
    private readonly List<Type> _widgets = [];

    /// <summary>
    /// Registers a concrete terminal screen entity type into the dynamic polymorphic serializer configuration map collection.
    /// </summary>
    /// <typeparam name="TScreen">The underlying concrete target screen entity implementation type derived from base root abstraction.</typeparam>
    /// <returns>The current instance of the resolver entity to guarantee fluid pipeline execution flow.</returns>
    public RedisJsonTypeResolver RegisterScreen<TScreen>() where TScreen : TerminalScreen
    {
        _screens.Add(typeof(TScreen));
        return this;
    }

    /// <summary>
    /// Registers a concrete command payload entity type into the dynamic polymorphic serializer configuration map collection.
    /// </summary>
    /// <typeparam name="TCommand">The underlying concrete target command entity implementation type derived from base root abstraction.</typeparam>
    /// <returns>The current instance of the resolver entity to guarantee fluid pipeline execution flow.</returns>
    public RedisJsonTypeResolver RegisterCommand<TCommand>() where TCommand : CommandBase
    {
        _commands.Add(typeof(TCommand));
        return this;
    }

    /// <summary>
    /// Registers a concrete visual render layout widget type into the dynamic polymorphic serializer configuration map collection.
    /// </summary>
    /// <typeparam name="TWidget">The underlying concrete target widget entity implementation type derived from base root abstraction.</typeparam>
    /// <returns>The current instance of the resolver entity to guarantee fluid pipeline execution flow.</returns>
    public RedisJsonTypeResolver RegisterWidget<TWidget>() where TWidget : TextWidget
    {
        _widgets.Add(typeof(TWidget));
        return this;
    }

    /// <summary>
    /// Compiles and generates global thread-safe serialization setting configurations carrying integrated dynamic metadata resolvers.
    /// </summary>
    /// <returns>A finalized instance of immutable serialization settings mapping layout properties.</returns>
    public JsonSerializerOptions CreateOptions()
    {
        DefaultJsonTypeInfoResolver resolver = new();

        resolver.Modifiers.Add(ConfigurePolymorphism);

        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = resolver,
            WriteIndented = false
        };

        return options;
    }

    private void ConfigurePolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(TerminalScreen) && _screens.Count > 0)
        {
            typeInfo.PolymorphismOptions = CreateOptionsForTypes(_screens);
        }
        else if (typeInfo.Type == typeof(CommandBase) && _commands.Count > 0)
        {
            typeInfo.PolymorphismOptions = CreateOptionsForTypes(_commands);
        }
        else if (typeInfo.Type == typeof(TextWidget) && _widgets.Count > 0)
        {
            typeInfo.PolymorphismOptions = CreateOptionsForTypes(_widgets);
        }
    }

    private static JsonPolymorphismOptions CreateOptionsForTypes(List<Type> derivedTypes)
    {
        JsonPolymorphismOptions options = new()
        {
            TypeDiscriminatorPropertyName = "$type",
            IgnoreUnrecognizedTypeDiscriminators = true,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        foreach (Type type in derivedTypes)
        {
            options.DerivedTypes.Add(new JsonDerivedType(type, type.Name));
        }

        return options;
    }
}
