using System.IO.Abstractions;

namespace Sequence.Utilities.SCLEditor.Components;

/// <summary>
/// Wrapper for a diff editor
/// </summary>
public class DiffEditorWrapper : IEditorWrapper
{
    /// <summary>
    /// Create a new DiffEditorOriginalWrapper
    /// </summary>
    public DiffEditorWrapper(
        MonacoDiffEditor monacoDiffEditor,
        EditorConfiguration configuration,
        IFileSystem? fileSystem,
        bool useOriginal)
    {
        MonacoDiffEditor = monacoDiffEditor;
        Configuration    = configuration;
        FileSystem       = fileSystem;
        UseOriginal      = useOriginal;
    }

    /// <summary>
    /// If true, use the original editor, else use the modified editor
    /// </summary>
    public bool UseOriginal { get; set; }

    private MonacoDiffEditor MonacoDiffEditor { get; }

    /// <inheritdoc />
    public EditorConfiguration Configuration { get; }

    /// <inheritdoc />
    public IFileSystem? FileSystem { get; }

    /// <inheritdoc />
    public async Task<TextModel> GetModelAsync()
    {
        var model = (await MonacoDiffEditor.GetModel());
        return UseOriginal ? model.Original : model.Modified;
    }

    /// <inheritdoc />
    public async Task<string> GetCodeAsync()
    {
        if (UseOriginal)
            return await MonacoDiffEditor.OriginalEditor.GetValue();

        return await MonacoDiffEditor.ModifiedEditor.GetValue();
    }

    /// <inheritdoc />
    public Task<List<Selection>> GetSelectionsAsync()
    {
        if (UseOriginal)
            return MonacoDiffEditor.OriginalEditor.GetSelections();

        return MonacoDiffEditor.ModifiedEditor.GetSelections();
    }

    /// <inheritdoc />
    public Task<bool> ExecuteEditsAsync(
        string source,
        List<IdentifiedSingleEditOperation> edits,
        List<Selection> selections)
    {
        if (UseOriginal)
            return MonacoDiffEditor.OriginalEditor.ExecuteEdits(source, edits, selections);

        return MonacoDiffEditor.ModifiedEditor.ExecuteEdits(source, edits, selections);
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
