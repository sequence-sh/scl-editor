using CSharpFunctionalExtensions;
using Reductech.Sequence.Connectors.FileSystem;
using Reductech.Sequence.Core;
using Reductech.Sequence.Core.Abstractions;
using Reductech.Sequence.Core.ExternalProcesses;
using Reductech.Sequence.Core.Internal;
using Reductech.Sequence.Core.Internal.Parser;
using Reductech.Sequence.Core.Internal.Serialization;
using Reductech.Sequence.Core.Util;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// Language Helper for the Reductech SCL language
/// </summary>
public class SCLLanguageHelper : ILanguageHelper
{
    private readonly IJSRuntime _runtime;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITestLoggerFactory _loggerFactory;

    private readonly Func<Task<StepFactoryStore>> _createStepFactoryStore;

    /// <summary>
    /// Create a new SCLLanguageHelper
    /// </summary>
    public SCLLanguageHelper(
        IJSRuntime runtime,
        IHttpClientFactory httpClientFactory,
        ITestLoggerFactory loggerFactory,
        Func<Task<StepFactoryStore>> createStepFactoryStore,
        StringBuilder consoleStream)
    {
        _runtime                = runtime;
        _httpClientFactory      = httpClientFactory;
        _loggerFactory          = loggerFactory;
        _createStepFactoryStore = createStepFactoryStore;
        ConsoleStream           = consoleStream;
    }

    /// <summary>
    /// Cancellation token for the currently running sequence
    /// </summary>
    public CancellationTokenSource? RunCancellation { get; private set; }

    private readonly ICompression _compression = new CompressionAdapter();
    private StepFactoryStore _stepFactoryStore = null!;

    private SCLCodeHelper _sclCodeHelper = null!;

    /// <summary>
    /// Stream of messages to the console
    /// </summary>
    public StringBuilder ConsoleStream { get; }

    /// <summary>
    /// Called when new console messages are sent
    /// </summary>
    public event EventHandler? OnNewConsoleMessage;

    /// <summary>
    /// Called when the state changes
    /// </summary>
    public event EventHandler? OnStateChanged;

    /// <summary>
    /// Called when a run starts
    /// </summary>
    public event EventHandler? OnRunStarted;

    /// <summary>
    /// Called when a run completes
    /// </summary>
    public event EventHandler? OnRunComplete;

    /// <summary>
    /// Called when a run is cancelled
    /// </summary>
    public event EventHandler? OnRunCancelled;

    /// <summary>
    /// Called when there is a new log message
    /// </summary>
    public event EventHandler? OnLogMessage;

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
    public async Task InitialSetup(IEditorWrapper editorWrapper)
    {
        if (_editor is not null)
        {
            throw new Exception("This SCL Language Helper has already been set up.");
        }

        _editor = editorWrapper;

        Console.SetOut(new StringWriter(ConsoleStream));
        _stepFactoryStore = await _createStepFactoryStore.Invoke();
        _sclCodeHelper    = new SCLCodeHelper(_stepFactoryStore, Editor.Configuration);
        var objRef = DotNetObjectReference.Create(_sclCodeHelper);

        //Function Defined in DefineSCLLanguage.js
        await _runtime.InvokeVoidAsync("registerSCL", objRef);

        var model = await Editor.GetModelAsync();
        await MonacoEditorBase.SetModelLanguage(model, "scl");

        await Editor.AddActionAsync(
            "runSCL",
            "Run SCL",
            new[] { (int)KeyMode.CtrlCmd | (int)KeyCode.KEY_R },
            null,
            null,
            "SCL",
            1.5,
            // ReSharper disable once AsyncVoidLambda
            async (_, _) => await Run()
        );

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
    }

    /// <summary>
    /// Called when the model content is changed
    /// </summary>
    public void OnDidChangeModelContent()
    {
        if (Editor.Configuration.DiagnosticsEnabled)
        {
            async void Action() => await Editor.SetDiagnostics(_runtime, _stepFactoryStore);
            _diagnosticsDebouncer.Dispatch(Action);
        }
    }

    private readonly Debouncer _diagnosticsDebouncer = new(TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Run the SCL
    /// </summary>
    public async Task Run()
    {
        OnRunStarted?.Invoke(this, EventArgs.Empty);

        var sclText = await Editor.GetCodeAsync();

        RunCancellation?.Cancel();
        var cts = new CancellationTokenSource();
        RunCancellation = cts;
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        var loggerSink = _loggerFactory.CreateLogger("SCL");
        var logger     = new ObservableLogger(loggerSink);

        OnLogMessage?.Invoke(this, EventArgs.Empty);

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, _stepFactoryStore));

        ConsoleStream.AppendLine("Sequence Running\n");
        OnNewConsoleMessage?.Invoke(this, EventArgs.Empty);

        if (stepResult.IsFailure)
        {
            ConsoleStream.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            List<(string, object)> injected = new();

            if (Editor.FileSystem is not null)
            {
                injected.Add((ConnectorInjection.FileSystemKey, Editor.FileSystem));
            }

            injected.Add((ConnectorInjection.CompressionKey, _compression));

            var externalContext = new ExternalContext(
                ExternalProcessRunner.Instance,
                new BlazorRestClientFactory(_httpClientFactory),
                ConsoleAdapter.Instance,
                injected.ToArray()
            );

            await using var stateMonad = new StateMonad(
                logger,
                _stepFactoryStore,
                externalContext,
                new Dictionary<string, object>()
            );

            var runResult = await stepResult.Value.Run<ISCLObject>(
                stateMonad,
                RunCancellation.Token
            );

            if (runResult.IsFailure)
                ConsoleStream.AppendLine(runResult.Error.AsString);
            else if (runResult.Value is Unit)
                ConsoleStream.AppendLine("\nSequence Completed Successfully");
            else
                ConsoleStream.AppendLine(runResult.Value.ToString());
        }

        RunCancellation = null;
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        ConsoleStream.AppendLine();
        OnNewConsoleMessage?.Invoke(this, EventArgs.Empty);

        OnRunComplete?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cancels SCL if it is running
    /// </summary>
    public void CancelRun()
    {
        RunCancellation?.Cancel();
        RunCancellation = null;
        OnRunCancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Apply formatting to the SCL in the editor
    /// </summary>
    public async Task FormatSCL()
    {
        var sclText = await Editor.GetCodeAsync();

        var selections = await Editor.GetSelectionsAsync();

        var uri = (await Editor.GetModelAsync()).Uri;

        var edits = Formatter.FormatDocument(sclText, _stepFactoryStore).ToList();

        await Editor.ExecuteEditsAsync(uri, edits, selections);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        CancelRun();
    }
}
