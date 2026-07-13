namespace PixelTerminalUI.Engine.Validators.ValidationProviders;

/// <summary>
/// Implements an immutable, thread-safe operational validation index provider that resolves runtime validation blocks
/// across parallel terminal processing pipes.
/// </summary>
/// <param name="registry">The snapshot directory instance containing pre-compiled user interface validation statements variables mapping.</param>
public sealed class ScreenValidationProvider(IReadOnlyDictionary<string, List<ValidationDelegate>> registry) : IScreenValidationProvider
{
    private readonly IReadOnlyDictionary<string, List<ValidationDelegate>> _registry = registry;

    /// <summary>
    /// Searches the internal index directory memory maps to expose evaluation delegate blocks assigned to the target
    /// terminal canvas identifier string.
    /// </summary>
    /// <param name="screenName">The explicit target terminal layout metadata signature identity name string.</param>
    /// <returns>An array tracking functional validation steps mapped onto the screen, or an empty collection if no rules have been initialized.</returns>
    public IEnumerable<ValidationDelegate> GetValidatorsForScreen(string screenName)
    {
        if (string.IsNullOrWhiteSpace(screenName))
        {
            return [];
        }
        return _registry.TryGetValue(screenName, out List<ValidationDelegate>? validators) ? validators : Array.Empty<ValidationDelegate>();
    }
}
