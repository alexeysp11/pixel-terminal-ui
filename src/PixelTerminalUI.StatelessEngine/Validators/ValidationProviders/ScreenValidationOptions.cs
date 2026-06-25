namespace PixelTerminalUI.StatelessEngine.Validators.ValidationProviders;

/// <summary>
/// Serves as a mutable staging option registry layer used to build, map, and organize screen validation chains during the framework initialization sequence.
/// </summary>
public sealed class ScreenValidationOptions
{
    private readonly Dictionary<string, List<ValidationDelegate>> _registry = new(StringComparer.Ordinal);

    /// <summary>
    /// Spawns or restores a dedicated fluent configuration context block assigned to attach custom business constraints onto the specified terminal form.
    /// </summary>
    /// <param name="screenName">The unique design-time layout configuration name targeting validation registration processes.</param>
    /// <returns>A validation builder instance tracking operational registration paths for the specified screen coordinate boundary.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided target terminal screen layout identity name string evaluates to null or empty whitespaces.</exception>
    public ScreenValidationBuilder ForScreen(string screenName)
    {
        if (string.IsNullOrWhiteSpace(screenName)) throw new ArgumentNullException(nameof(screenName));

        if (!_registry.TryGetValue(screenName, out List<ValidationDelegate>? validators))
        {
            validators = [];
            _registry[screenName] = validators;
        }

        return new ScreenValidationBuilder(validators);
    }

    /// <summary>
    /// Compiles the mutable staging memory structures into a stable, read-only dictionary mapping layout metadata identifiers onto specific validation vectors.
    /// </summary>
    /// <returns>A thread-safe read-only snapshot tracking all structural validation parameters defined during system configuration loops.</returns>
    public IReadOnlyDictionary<string, List<ValidationDelegate>> BuildRegistry() => _registry;
}
