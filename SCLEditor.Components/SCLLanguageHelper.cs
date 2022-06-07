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
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ITestLoggerFactory _loggerFactory;

    private readonly Func<Task<StepFactoryStore>> _createStepFactoryStore;

    /// <summary>
    /// Create a new SCLLanguageHelper
    /// </summary>
    public SCLLanguageHelper(
        IJSRuntime runtime,
        IHttpClientFactory? httpClientFactory,
        ITestLoggerFactory loggerFactory,
        Func<Task<StepFactoryStore>> createStepFactoryStore)
    {
        _runtime                = runtime;
        _httpClientFactory      = httpClientFactory;
        _loggerFactory          = loggerFactory;
        _createStepFactoryStore = createStepFactoryStore;
    }

    public StringBuilder ConsoleStream { get; set; } = new();

    public Action<bool>? OnNewConsoleMessage { get; set; }

    public Action? OnStateHasChanged { get; set; }

    public Func<Task>? OnRunStarted { get; set; }

    public Func<Task>? OnRunComplete { get; set; }

    public Action? OnRunCancelled { get; set; }

    public PropertyChangedEventHandler? OnNewLogMessage { get; set; }

    public CancellationTokenSource? RunCancellation { get; private set; }

    private readonly ICompression _compression = new CompressionAdapter();

    private StepFactoryStore _stepFactoryStore = null!;

    private SCLCodeHelper _sclCodeHelper = null!;

    private Editor _editor = null!;

    #pragma warning disable CS1998
    public async Task OnInitializedAsync(Editor editor)
        #pragma warning restore CS1998
    {
        _editor = editor;
    }

    public async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (_editor.Configuration is not EditorConfigurationSCL config)
                throw new ArgumentNullException(
                    nameof(_editor.Configuration),
                    $"{nameof(_editor.Configuration)} is required for the {nameof(SCLLanguageHelper)}."
                );

            Console.SetOut(new StringWriter(ConsoleStream));

            _stepFactoryStore = await _createStepFactoryStore.Invoke();

            _sclCodeHelper = new SCLCodeHelper(_stepFactoryStore, config);

            var objRef = DotNetObjectReference.Create(_sclCodeHelper);

            //Function Defined in DefineSCLLanguage.js
            await _runtime.InvokeVoidAsync("registerSCL", objRef);

            var model = await _editor.Instance.GetModel();
            await MonacoEditorBase.SetModelLanguage(model, "scl");

            await _editor.Instance.AddAction(
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

            await _editor.Instance.AddAction(
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
    }

    /// <summary>
    /// Called when the model content is changed
    /// </summary>
    public void OnDidChangeModelContent()
    {
        if (_editor.Configuration is EditorConfigurationSCL { DiagnosticsEnabled: true })
        {
            _diagnosticsDebouncer.Dispatch(
                async () => await _sclCodeHelper.SetDiagnostics(_editor.Instance, _runtime)
            );
        }
    }

    private readonly Debouncer _diagnosticsDebouncer = new(TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Run the SCL
    /// </summary>
    public async Task Run()
    {
        if (OnRunStarted is not null)
            await OnRunStarted.Invoke();

        var sclText = await _editor.Instance.GetValue();

        RunCancellation?.Cancel();
        var cts = new CancellationTokenSource();
        RunCancellation = cts;
        OnStateHasChanged?.Invoke();

        var loggerSink = _loggerFactory.CreateLogger("SCL");
        var logger     = new ObservableLogger(loggerSink);

        if (OnNewLogMessage is not null)
            logger.PropertyChanged += OnNewLogMessage;

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, _stepFactoryStore));

        ConsoleStream.AppendLine("Sequence Running\n");
        OnNewConsoleMessage?.Invoke(true);

        if (stepResult.IsFailure)
        {
            ConsoleStream.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            List<(string, object)> injected = new();

            if (_editor.FileSystem?.FileSystem is not null)
            {
                injected.Add((ConnectorInjection.FileSystemKey, _editor.FileSystem.FileSystem));
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
        OnStateHasChanged?.Invoke();

        ConsoleStream.AppendLine();
        OnNewConsoleMessage?.Invoke(true);

        if (OnRunComplete is not null)
            await OnRunComplete.Invoke();
    }

    public void CancelRun()
    {
        RunCancellation?.Cancel();
        RunCancellation = null;
        OnRunCancelled?.Invoke();
    }

    public async Task FormatSCL()
    {
        var sclText = await _editor.Instance.GetValue();

        var selections = await _editor.Instance.GetSelections();

        var uri = (await _editor.Instance.GetModel()).Uri;

        var edits = Formatter.FormatDocument(sclText, _stepFactoryStore).ToList();

        await _editor.Instance.ExecuteEdits(uri, edits, selections);
    }

    public void Dispose()
    {
        CancelRun();
    }
}
