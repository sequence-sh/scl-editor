namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// Handles interactions with the monaco editor
/// </summary>
public interface ILanguageHelper : IDisposable
{
    /// <summary>
    /// Called after the editor is rendered
    /// </summary>
    public Task<bool> InitialSetup(IEditorWrapper editorWrapper);

    /// <summary>
    /// Called when the model content is changed
    /// </summary>
    public void OnDidChangeModelContent();
}

/// <summary>
/// A language helper that does nothing
/// </summary>
public class DefaultLanguageHelper : ILanguageHelper
{
    private DefaultLanguageHelper() { }

    /// <summary>
    /// The instance
    /// </summary>
    public static ILanguageHelper Instance { get; } = new DefaultLanguageHelper();

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    #pragma warning disable CS1998
    public async Task<bool> InitialSetup(IEditorWrapper editorWrapper)
        #pragma warning restore CS1998
    {
        return true;
    }

    /// <inheritdoc />
    public void OnDidChangeModelContent()
    {
        //Do nothing
    }
}
