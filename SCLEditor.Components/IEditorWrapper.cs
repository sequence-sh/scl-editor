using System.IO.Abstractions;
using Sequence.Core.Internal;
using Sequence.Core.LanguageServer;
using Sequence.SCLEditor.Components.Objects;

namespace Sequence.SCLEditor.Components;

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
    public async Task SetDiagnostics(
        IJSRuntime runtime,
        StepFactoryStore stepFactoryStore,
        IReadOnlyDictionary<VariableName, InjectedVariable>? injectedVariables = null)
    {
        if (!Configuration.DiagnosticsEnabled)
            return;

        var uri  = (await GetModelAsync()).Uri;
        var code = await GetCodeAsync();

        var diagnostics =
            DiagnosticsHelper.GetDiagnostics(code, stepFactoryStore, injectedVariables)
                .Select(x => new VSDiagnostic(x))
                .ToList();

        await runtime.InvokeAsync<string>("setDiagnostics", diagnostics, uri);
    }
}
