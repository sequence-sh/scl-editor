using System.IO.Abstractions;
using Reductech.Sequence.Core.Internal;
using Reductech.Sequence.Core.LanguageServer;
using Reductech.Utilities.SCLEditor.Components.Objects;

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

/*
/// <summary>
/// Wrapper for a diff editor
/// </summary>
public class DiffEditorOriginalWrapper : IEditorWrapper
{
    /// <summary>
    /// Create a new DiffEditorOriginalWrapper
    /// </summary>
    public DiffEditorOriginalWrapper(
        MonacoDiffEditor monacoDiffEditor,
        EditorConfiguration configuration,
        IFileSystem? fileSystem)
    {
        MonacoDiffEditor = monacoDiffEditor;
        Configuration    = configuration;
        FileSystem       = fileSystem;
    }

    private MonacoDiffEditor MonacoDiffEditor { get; }

    /// <inheritdoc />
    public EditorConfiguration Configuration { get; }

    /// <inheritdoc />
    public IFileSystem? FileSystem { get; }

    /// <inheritdoc />
    public async Task<TextModel> GetModelAsync()
    {
        return (await MonacoDiffEditor.GetModel()).Original;
    }

    /// <inheritdoc />
    public async Task<string> GetCodeAsync()
    {
        return await MonacoDiffEditor.OriginalEditor.GetValue();
    }

    /// <inheritdoc />
    public Task<List<Selection>> GetSelectionsAsync()
    {
        return MonacoDiffEditor.OriginalEditor.GetSelections();
    }

    /// <inheritdoc />
    public Task<bool> ExecuteEditsAsync(
        string source,
        List<IdentifiedSingleEditOperation> edits,
        List<Selection> selections)
    {
        return MonacoDiffEditor.OriginalEditor.ExecuteEdits(source, edits, selections);
    }

    /// <inheritdoc />
    public Task AddActionAsync(
        string actionId,
        string label,
        int[] keyCodes,
        string precondition,
        string keybindingContext,
        string contextMenuGroupId,
        double contextMenuOrder,
        Action<MonacoEditorBase, int[]> action)
    {
        return MonacoDiffEditor.AddAction(
            actionId,
            label,
            keyCodes,
            precondition,
            keybindingContext,
            contextMenuGroupId,
            contextMenuOrder,
            action
        );
    }
}

*/

/// <summary>
/// Wrapper for a monaco editor
/// </summary>
public class MonacoEditorWrapper : IEditorWrapper
{
    /// <summary>
    /// Create a new MonacoEditorWrapper
    /// </summary>
    public MonacoEditorWrapper(
        MonacoEditor monacoEditor,
        EditorConfiguration configuration,
        IFileSystem? fileSystem)
    {
        MonacoEditor  = monacoEditor;
        Configuration = configuration;
        FileSystem    = fileSystem;
    }

    private MonacoEditor MonacoEditor { get; }

    /// <inheritdoc />
    public IFileSystem? FileSystem { get; }

    /// <inheritdoc />
    public EditorConfiguration Configuration { get; }

    /// <inheritdoc />
    public Task<TextModel> GetModelAsync()
    {
        return MonacoEditor.GetModel();
    }

    /// <inheritdoc />
    public Task<List<Selection>> GetSelectionsAsync()
    {
        return MonacoEditor.GetSelections();
    }

    /// <inheritdoc />
    public Task<bool> ExecuteEditsAsync(
        string source,
        List<IdentifiedSingleEditOperation> edits,
        List<Selection> selections)
    {
        return MonacoEditor.ExecuteEdits(source, edits, selections);
    }

    /// <inheritdoc />
    public Task<string> GetCodeAsync()
    {
        return MonacoEditor.GetValue();
    }

    /// <inheritdoc />
    public Task AddActionAsync(
        string actionId,
        string label,
        int[] keyCodes,
        string? precondition,
        string? keybindingContext,
        string contextMenuGroupId,
        double contextMenuOrder,
        Action<MonacoEditorBase, int[]> action)
    {
        return MonacoEditor.AddAction(
            actionId,
            label,
            keyCodes,
            precondition,
            keybindingContext,
            contextMenuGroupId,
            contextMenuOrder,
            action
        );
    }
}

/// <summary>
/// Wraps a monaco editor
/// </summary>
public interface IEditorWrapper
{
    /// <summary>
    /// The file system that this editor uses
    /// </summary>
    public IFileSystem? FileSystem { get; }

    /// <summary>
    /// Get the editor model
    /// </summary>
    public Task<TextModel> GetModelAsync();

    /// <summary>
    /// Gets the code from the editor
    /// </summary>
    /// <returns></returns>
    public Task<string> GetCodeAsync();

    /// <summary>
    /// Get Selected Code
    /// </summary>
    /// <returns></returns>
    public Task<List<Selection>> GetSelectionsAsync();

    /// <summary>
    /// Execute SCL edits
    /// </summary>
    public Task<bool> ExecuteEditsAsync(
        string source,
        List<IdentifiedSingleEditOperation> edits,
        List<Selection> selections);

    /// <summary>
    /// Add an action to the editor
    /// </summary>
    public Task AddActionAsync(
        string actionId,
        string label,
        int[] keyCodes,
        string? precondition,
        string? keybindingContext,
        string contextMenuGroupId,
        double contextMenuOrder,
        Action<MonacoEditorBase, int[]> action);

    /// <summary>
    /// The SCL configuration
    /// </summary>
    public EditorConfiguration Configuration { get; }

    /// <summary>
    /// Sets editor diagnostics
    /// </summary>
    public async Task SetDiagnostics(IJSRuntime runtime, StepFactoryStore stepFactoryStore)
    {
        if (!Configuration.DiagnosticsEnabled)
            return;

        var uri  = (await GetModelAsync()).Uri;
        var code = await GetCodeAsync();

        var diagnostics =
            DiagnosticsHelper.GetDiagnostics(code, stepFactoryStore)
                .Select(x => new VSDiagnostic(x))
                .ToList();

        await runtime.InvokeAsync<string>("setDiagnostics", diagnostics, uri);
    }
}
