namespace PixelTerminalUI.StatelessEngine.Validators.ValidationProviders;

/// <summary>
/// Defines a thread-safe registry directory contract responsible for exposing validation rules assigned to targeted workflow interfaces.
/// </summary>
public interface IScreenValidationProvider
{
    /// <summary>
    /// Resolves an ordered sequence of functional validation statement delegates associated with the targeted user interface form name identifier.
    /// </summary>
    /// <param name="screenName">The explicit target terminal layout metadata signature identity name string.</param>
    /// <returns>A collection of registered data evaluation rules matching the requested form context parameters.</returns>
    IEnumerable<ValidationDelegate> GetValidatorsForScreen(string screenName);
}
