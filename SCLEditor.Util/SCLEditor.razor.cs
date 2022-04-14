using System.Reactive.Linq;
using Reductech.Sequence.Connectors.FileSystem.Steps;

namespace Reductech.Utilities.SCLEditor.Util;

public partial class SCLEditor : Editor
{
    /// <summary>
    /// The JS runtime
    /// </summary>
    [Inject]
    public IJSRuntime Runtime { get; set; } = null!;

    /// <summary>
    /// The HttpClient Factory
    /// </summary>
    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; } = null!;

    [Parameter][EditorRequired] public StringBuilder OutputConsole { get; set; } = null!;

    [Parameter][EditorRequired] public ITestLoggerFactory OutputLog { get; set; } = null!;

    [Parameter] public PropertyChangedEventHandler? OnNewLogMessage { get; set; }

    [Parameter] public Action<bool>? OnNewOutputMessage { get; set; }

    [Parameter]
    public override Func<MonacoEditor, StandaloneEditorConstructionOptions>
        ConstructionOptions { get; set; } = (MonacoEditor _) => new()
    {
        AutomaticLayout = true,
        Language        = "scl",
        Minimap         = new EditorMinimapOptions { Enabled = false }
    };

    internal CancellationTokenSource? RunCancellation { get; private set; }

    private readonly ICompression _compression = new CompressionAdapter();

    private StepFactoryStore _stepFactoryStore = null!;

    private SCLCodeHelper _sclCodeHelper = null!;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await SetupSCL();
        }
    }

    private async Task SetupSCL()
    {
        if (FileSystem is null)
            throw new ArgumentNullException(
                nameof(FileSystem),
                "FileSystem is required for the SCLEditor."
            );

        if (Configuration is not SCLEditorConfiguration config)
            throw new ArgumentNullException(
                nameof(Configuration),
                "Configuration is required for the SCLEditor."
            );

        Console.SetOut(new StringWriter(OutputConsole));

        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            ExternalContext.Default,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        _stepFactoryStore = stepFactoryStoreResult.Value;

        _sclCodeHelper = new SCLCodeHelper(_stepFactoryStore, config);

        var objRef = DotNetObjectReference.Create(_sclCodeHelper);

        //Function Defined in DefineSCLLanguage.js
        await Runtime.InvokeVoidAsync("registerSCL", objRef);

        var model = await Instance.GetModel();
        await MonacoEditorBase.SetModelLanguage(model, "scl");

        await Instance.AddAction(
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

        await Instance.AddAction(
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

    private readonly Debouncer _diagnosticsDebouncer = new(TimeSpan.FromMilliseconds(300));

    protected override void OnDidChangeModelContent()
    {
        base.OnDidChangeModelContent();

        #pragma warning disable CS4014
        _diagnosticsDebouncer.Dispatch(() => _sclCodeHelper.SetDiagnostics(Instance, Runtime));
        #pragma warning restore CS4014
    }

    internal async Task Run()
    {
        var sclText = await Instance.GetValue();

        RunCancellation?.Cancel();
        var cts = new CancellationTokenSource();
        RunCancellation = cts;
        StateHasChanged(); // Update the Run / Stop button

        var loggerSink = OutputLog.CreateLogger("SCL");
        var logger     = new ObservableLogger(loggerSink);

        if (OnNewLogMessage is not null)
            logger.PropertyChanged += OnNewLogMessage;

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, _stepFactoryStore));

        OutputConsole.AppendLine("Sequence Running\n");
        OnNewOutputMessage?.Invoke(true);

        if (stepResult.IsFailure)
        {
            OutputConsole.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            var externalContext = new ExternalContext(
                ExternalProcessRunner.Instance,
                new BlazorRestClientFactory(HttpClientFactory),
                ConsoleAdapter.Instance,
                (ConnectorInjection.FileSystemKey, FileSystem!.FileSystem),
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
                OutputConsole.AppendLine(runResult.Error.AsString);
            else if (runResult.Value is Unit)
                OutputConsole.AppendLine("\nSequence Completed Successfully");
            else
                OutputConsole.AppendLine(runResult.Value.ToString());
        }

        RunCancellation = null;
        StateHasChanged(); // Update the Run / Stop button

        OutputConsole.AppendLine();
        OnNewOutputMessage?.Invoke(true);
    }

    internal void CancelRun()
    {
        RunCancellation?.Cancel();
        RunCancellation = null;
    }

    internal async Task FormatSCL()
    {
        var sclText = await Instance.GetValue();

        var selections = await Instance.GetSelections();

        var uri = (await Instance.GetModel()).Uri;

        var edits = Formatter.FormatDocument(sclText, _stepFactoryStore).ToList();

        await Instance.ExecuteEdits(uri, edits, selections);
    }
}
