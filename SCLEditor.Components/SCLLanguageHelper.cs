using Reductech.Sequence.Core.Internal;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// Language Helper for the Reductech SCL language
/// </summary>
public class SCLLanguageHelper : ILanguageHelper
{
    private readonly IJSRuntime _runtime;

    private readonly Func<Task<StepFactoryStore>> _createStepFactoryStore;

    /// <summary>
    /// Create a new SCLLanguageHelper
    /// </summary>
    public SCLLanguageHelper(
        IJSRuntime runtime,
        Func<Task<StepFactoryStore>> createStepFactoryStore)
    {
        _runtime                = runtime;
        _createStepFactoryStore = createStepFactoryStore;
    }

    /// <summary>
    /// The step factory store
    /// </summary>
    protected StepFactoryStore StepFactoryStore = null!;

    private SCLCodeHelper _sclCodeHelper = null!;

    /// <summary>
    /// The editor wrapper
    /// </summary>
    public IEditorWrapper Editor
    {
        get
        {
            if (_editor is null)
            {
                throw new Exception("This SCL language helper has not been set up");
            }

            return _editor;
        }
    }

    private IEditorWrapper? _editor;

    /// <inheritdoc />
    public virtual async Task<bool> InitialSetup(IEditorWrapper editorWrapper)
    {
        if (_editor is not null)
        {
            return false;
            //throw new Exception("This SCL Language Helper has already been set up.");
        }

        _editor = editorWrapper;

        StepFactoryStore = await _createStepFactoryStore.Invoke();
        _sclCodeHelper   = new SCLCodeHelper(StepFactoryStore, Editor.Configuration);
        var objRef = DotNetObjectReference.Create(_sclCodeHelper);

        //Function Defined in DefineSCLLanguage.js
        await _runtime.InvokeVoidAsync("registerSCL", objRef);

        var model = await Editor.GetModelAsync();
        await MonacoEditorBase.SetModelLanguage(model, "scl");

        await Editor.AddActionAsync(
            "formatscl",
            "Format SCL",
            new[] { (int)KeyMode.CtrlCmd | (int)KeyCode.KEY_F },
            null,
            null,
            "SCL",
            1.5,
            // ReSharper disable once AsyncVoidLambda
            async (_, _) => await FormatSCL()
        );

        return true;
    }

    /// <summary>
    /// Called when the model content is changed
    /// </summary>
    public void OnDidChangeModelContent()
    {
        if (Editor.Configuration.DiagnosticsEnabled)
        {
            async void Action() => await Editor.SetDiagnostics(_runtime, StepFactoryStore);
            _diagnosticsDebouncer.Dispatch(Action);
        }
    }

    private readonly Debouncer _diagnosticsDebouncer = new(TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Apply formatting to the SCL in the editor
    /// </summary>
    public async Task FormatSCL()
    {
        var sclText = await Editor.GetCodeAsync();

        var selections = await Editor.GetSelectionsAsync();

        var uri = (await Editor.GetModelAsync()).Uri;

        var edits = Formatter.FormatDocument(sclText, StepFactoryStore).ToList();

        await Editor.ExecuteEditsAsync(uri, edits, selections);
    }

    /// <inheritdoc/>
    public virtual void Dispose() { }
}
