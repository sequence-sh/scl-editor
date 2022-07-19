namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// Handles interactions with the monaco editor
/// </summary>
public interface ILanguageHelper : IDisposable
{
    /// <summary>
    /// Called after the editor is rendered
    /// </summary>
    public Task InitialSetup(IEditorWrapper editorWrapper);

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
    public Task InitialSetup(IEditorWrapper editorWrapper)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void OnDidChangeModelContent()
    {
        //Do nothing
    }
}
