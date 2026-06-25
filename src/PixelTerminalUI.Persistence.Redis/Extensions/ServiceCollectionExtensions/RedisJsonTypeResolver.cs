using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using PixelTerminalUI.StatelessEngine.Commands.Core;

namespace PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;

public sealed class RedisJsonTypeResolver
{
    private readonly List<Type> _screens = [];
    private readonly List<Type> _commands = [];
    private readonly List<Type> _widgets = [];

    public RedisJsonTypeResolver RegisterScreen<TScreen>() where TScreen : TerminalScreen
    {
        _screens.Add(typeof(TScreen));
        return this;
    }

    public RedisJsonTypeResolver RegisterCommand<TCommand>() where TCommand : CommandBase
    {
        _commands.Add(typeof(TCommand));
        return this;
    }

    public RedisJsonTypeResolver RegisterWidget<TWidget>() where TWidget : TextWidget
    {
        _widgets.Add(typeof(TWidget));
        return this;
    }

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
