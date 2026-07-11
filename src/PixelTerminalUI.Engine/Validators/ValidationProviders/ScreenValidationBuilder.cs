namespace PixelTerminalUI.Engine.Validators.ValidationProviders;

/// <summary>
/// Provides a fluid configuration context block utilized to safely attach validation chains onto targeted collection containers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ScreenValidationBuilder"/> class.
/// </remarks>
/// <param name="validators">The concrete mutable tracking list where functional validation delegates are stored.</param>
public sealed class ScreenValidationBuilder(List<ValidationDelegate> validators)
{
    private readonly List<ValidationDelegate> _validators = validators ?? throw new ArgumentNullException(nameof(validators));

    /// <summary>
    /// Appends a new stateless operational validation rule delegate variable onto the configured interface screen pipeline stack.
    /// </summary>
    /// <param name="validator">The functional evaluation statement tracking input validation properties logic.</param>
    /// <returns>The active configuration builder instance to sustain continuous processing chaining paths.</returns>
    public ScreenValidationBuilder Add(ValidationDelegate validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _validators.Add(validator);
        return this;
    }
}
