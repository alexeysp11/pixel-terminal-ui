namespace PixelTerminalUI.StatelessEngine.Validators;

public readonly record struct ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Fail(string error) => new(false, error);
}
