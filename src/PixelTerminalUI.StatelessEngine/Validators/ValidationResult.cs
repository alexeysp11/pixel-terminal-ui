namespace PixelTerminalUI.StatelessEngine.Validators;

/// <summary>
/// Represents the deterministic outcome of a structural validation operation containing state metadata.
/// </summary>
public readonly record struct ValidationResult(bool IsValid, string? ErrorMessage)
{
    /// <summary>
    /// Initializes a successful validation result instance with no error payloads.
    /// </summary>
    /// <returns>A validation result instance signifying successful operational evaluation parameters.</returns>
    public static ValidationResult Success() => new(true, null);

    /// <summary>
    /// Initializes a failed validation result instance bound to a specific diagnostic message.
    /// </summary>
    /// <param name="error">The concrete contextual error message describing the validation breakdown reason.</param>
    /// <returns>A validation result instance carrying specific failure indicator flags and diagnostics.</returns>
    public static ValidationResult Fail(string error) => new(false, error);
}
