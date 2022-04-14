using Reductech.Sequence.Connectors.FileSystem.Steps;

namespace Reductech.Utilities.SCLEditor.Util;

public class EditorSCLHelper
{
    private readonly IJSRuntime _runtime;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITestLoggerFactory _loggerFactory;

    //private readonly CompoundFileSystem _fileSystem;

    public EditorSCLHelper(
            IJSRuntime runtime,
            IHttpClientFactory httpClientFactory,
            ITestLoggerFactory loggerFactory)
        //CompoundFileSystem fileSystem)
    {
        _runtime           = runtime;
        _httpClientFactory = httpClientFactory;
        _loggerFactory     = loggerFactory;
        //_fileSystem        = fileSystem;
    }

    //public Editor EditorInstance { get; set; } = null!;

    //public MonacoEditor MonacoEditor { get; set; } = null!;

    //public EditorConfigurationSCL Configuration { get; set; } = new();

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

    private bool _notInit = true;

    //private MonacoEditor _editor = null!;
    private Editor _editor = null!;

    public async Task Init(Editor editor)
    {
        _editor = editor;

        if (_editor.FileSystem is null)
            throw new ArgumentNullException(
                nameof(_editor.FileSystem),
                $"{nameof(_editor.FileSystem)} is required to initializse the {nameof(EditorSCLHelper)}."
            );

        if (_editor.Configuration is not EditorConfigurationSCL config)
            throw new ArgumentNullException(
                nameof(_editor.Configuration),
                $"{nameof(_editor.Configuration)} is required for the {nameof(EditorSCLHelper)}."
            );

        Console.SetOut(new StringWriter(ConsoleStream));

        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            ExternalContext.Default,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        _stepFactoryStore = stepFactoryStoreResult.Value;

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

        _notInit = false;
    }

    internal async Task Run()
    {
        if (_notInit)
            throw new Exception(
                $"{nameof(EditorSCLHelper)} is not initialized. Run the {nameof(Init)} method first."
            );

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
            var externalContext = new ExternalContext(
                ExternalProcessRunner.Instance,
                new BlazorRestClientFactory(_httpClientFactory),
                ConsoleAdapter.Instance,
                (ConnectorInjection.FileSystemKey, _editor.FileSystem!.FileSystem),
                (ConnectorInjection.CompressionKey, _compression)
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

    internal void CancelRun()
    {
        RunCancellation?.Cancel();
        RunCancellation = null;
        OnRunCancelled?.Invoke();
    }

    internal async Task FormatSCL()
    {
        if (_notInit)
            throw new Exception(
                $"{nameof(EditorSCLHelper)} is not initialized. Run the {nameof(Init)} method first."
            );

        var sclText = await _editor.Instance.GetValue();

        var selections = await _editor.Instance.GetSelections();

        var uri = (await _editor.Instance.GetModel()).Uri;

        var edits = Formatter.FormatDocument(sclText, _stepFactoryStore).ToList();

        await _editor.Instance.ExecuteEdits(uri, edits, selections);
    }
}
