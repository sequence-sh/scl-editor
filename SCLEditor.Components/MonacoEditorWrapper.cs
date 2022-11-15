using System.IO.Abstractions;

namespace Sequence.SCLEditor.Components;

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
