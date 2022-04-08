using System.Reactive.Linq;
using System.Reactive.Subjects;
using MudBlazor;
using Reductech.Sequence.Connectors.FileSystem.Steps;

namespace Reductech.Utilities.SCLEditor.Util;

/// <summary>
/// Playground for using SCL
/// </summary>
public sealed partial class Playground : IDisposable
{
    /// <summary>
    /// The JS runtime
    /// </summary>
    [Inject]
    public IJSRuntime Runtime { get; set; } = null!;

    /// <summary>
    /// The Dialog Service
    /// </summary>
    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    /// <summary>
    /// The File System
    /// </summary>
    [Inject]
    public CompoundFileSystem FileSystem { get; set; } = null!;

    /// <summary>
    /// The HttpClient Factory
    /// </summary>
    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; } = null!;

    /// <summary>
    /// Whether this component should be rendered with dark mode
    /// </summary>
    [Parameter]
    public bool IsDarkMode
    {
        get => Themes.IsDarkMode.Value;
        set => Themes.IsDarkMode.OnNext(value);
    }

    private MudTheme CurrentTheme { get; set; } = Themes.DefaultTheme;

    /// <summary>
    /// The _scl editor to use
    /// </summary>
    private MonacoEditor _sclEditor = null!;

    private MonacoEditor? _fileEditor = null!;

    bool OutputExpanded { get; set; } = true;
    bool LogExpanded { get; set; } = true;

    private readonly ITestLoggerFactory _testLoggerFactory =
        TestLoggerFactory.Create(x => x.FilterByMinimumLevel(LogLevel.Information));

    private readonly ICompression _compression = new CompressionAdapter();

    private readonly StringBuilder _consoleStringBuilder = new();

    private StepFactoryStore _stepFactoryStore = null!;

    private FileSelection _fileSelection = null!;

    private CancellationTokenSource? RunCancellation { get; set; }

    private SCLCodeHelper _sclCodeHelper = null!;

    private EditorConfiguration _configuration = new();

    private FileData? _openedFile = null;

    private bool _hotChanges = false;
    private MudMessageBox MudMessageBox { get; set; } = null!;

    private string? _title = null;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Console.SetOut(new StringWriter(_consoleStringBuilder));

        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            ExternalContext.Default,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        Themes.IsDarkMode
            .TakeUntil(_disposed)
            .Select(x => Observable.FromAsync(() => SetTheme(x)))
            .Switch()
            .Subscribe();

        _stepFactoryStore = stepFactoryStoreResult.Value;
    }

    private async Task SetTheme(bool isDarkMode)
    {
        CurrentTheme = isDarkMode ? Themes.DarkTheme : Themes.DefaultTheme;
        var theme = isDarkMode ? "vs-dark" : "vs";

        await MonacoEditorBase.SetTheme(theme);

        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var containsConfigKey =
                await FileSystem.LocalStorage.ContainKeyAsync(EditorConfiguration.ConfigurationKey);

            if (containsConfigKey)
                _configuration = await
                    FileSystem.LocalStorage.GetItemAsync<EditorConfiguration>(
                        EditorConfiguration.ConfigurationKey
                    );
            else
                _configuration = new EditorConfiguration();

            _configuration.PropertyChanged += Configuration_PropertyChanged;

            _sclCodeHelper = new SCLCodeHelper(_stepFactoryStore, _configuration);

            var objRef = DotNetObjectReference.Create(_sclCodeHelper);

            await Runtime.InvokeVoidAsync(
                "registerSCL",
                objRef
            ); //Function Defined in DefineSCLLanguage.js

            var model = await _sclEditor.GetModel();
            await MonacoEditorBase.SetModelLanguage(model, "scl");

            await _sclEditor.AddAction(
                "runSCL",
                "Run SCL",
                new[] { (int)KeyMode.CtrlCmd | (int)KeyCode.KEY_R },
                null,
                null,
                "SCL",
                1.5,
                // ReSharper disable once AsyncVoidLambda
                async (_, _) =>
                {
                    await Run();
                    StateHasChanged();
                }
            );

            await _sclEditor.AddAction(
                "formatscl",
                "Format SCL",
                new[] { (int)KeyMode.CtrlCmd | (int)KeyCode.KEY_F },
                null,
                null,
                "SCL",
                1.5,
                // ReSharper disable once AsyncVoidLambda
                async (_, _) =>
                {
                    await FormatSCL();
                }
            );
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private readonly Subject<bool> _disposed = new();

    public void Dispose()
    {
        _disposed.OnNext(true);
    }

    private async Task SaveSCLFile()
    {
        if (_title is null)
        {
            var change = await MudMessageBox.Show(new DialogOptions());

            if (change != true)
                return;

            if (_title is null)
                return;
        }

        if (!_title.EndsWith(".scl", StringComparison.InvariantCultureIgnoreCase))
        {
            _title += ".scl";
        }

        _hotChanges = false;

        await _fileSelection.FileSystem.SaveFile(_sclEditor, _title);
    }

    private void CloseOpenFile()
    {
        _openedFile = null;
    }

    private async Task SaveOpenFile()
    {
        if (_fileEditor is not null && _openedFile is not null)
        {
            await FileSystem.SaveFile(_fileEditor, _openedFile.Path);
        }
    }

    private async Task OpenFileAction(FileData arg)
    {
        if (Path.GetExtension(arg.Path) == ".scl")
        {
            _title      = arg.Path;
            _hotChanges = false;
            await _sclEditor.SetValue(arg.Data.TextContents);
        }
        else
        {
            CloseOpenFile();

            _openedFile = arg;

            if (_fileEditor is not null)
                await _fileEditor.SetValue(arg.Data.TextContents);

            StateHasChanged();
        }
    }

    private async Task SaveConfiguration()
    {
        await FileSystem.LocalStorage.SetItemAsync(
            EditorConfiguration.ConfigurationKey,
            _configuration
        );
    }

    private void Configuration_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        #pragma warning disable CS4014
        SaveConfiguration();
        #pragma warning restore CS4014
    }

    private int _activeTabIndex;

    private const int OutputTabIndex = 0;
    private const int LogTabIndex = 1;

    public int ActiveOutputTabPanel
    {
        get => _activeTabIndex;
        set
        {
            if (value == _activeTabIndex)
                return;

            switch (value)
            {
                case OutputTabIndex:
                    SetOutputBadge(false);
                    break;
                case LogTabIndex:
                    SetLogBadge(false);
                    break;
            }

            _activeTabIndex = value;
        }
    }

    private string? _outputBadge = null;
    private bool _outputDot = false;

    private void SetOutputBadge(bool display)
    {
        if (display)
        {
            if (_activeTabIndex != OutputTabIndex)
            {
                _outputBadge = string.Empty;
                _outputDot   = true;
            }
        }
        else
        {
            _outputBadge = null;
            _outputDot   = false;
        }
    }

    private string? _logBadge = null;
    private bool _logDot = false;

    private void SetLogBadge(bool display)
    {
        if (display)
        {
            if (_activeTabIndex != LogTabIndex)
            {
                _logBadge = string.Empty;
                _logDot   = true;
            }
        }
        else
        {
            _logBadge = null;
            _logDot   = false;
        }
    }

    private void ClearOutput()
    {
        _consoleStringBuilder.Clear();
        SetOutputBadge(false);
    }

    private void ClearLogs()
    {
        _testLoggerFactory.Sink.Clear();
        SetLogBadge(false);
    }

    private string LogText()
    {
        var text = string.Join("\r\n", _testLoggerFactory.Sink.LogEntries.Select(x => x.Message));
        return text;
    }

    private readonly Debouncer _diagnosticsDebouncer = new(TimeSpan.FromMilliseconds(300));

    private void OnDidChangeModelContent()
    {
        _hotChanges = true;
        #pragma warning disable CS4014
        _diagnosticsDebouncer.Dispatch(() => _sclCodeHelper.SetDiagnostics(_sclEditor, Runtime));
        #pragma warning restore CS4014
    }

    private void CancelRun()
    {
        RunCancellation?.Cancel();
        RunCancellation = null;
    }

    private async Task FormatSCL()
    {
        var sclText = await _sclEditor.GetValue();

        var selections = await _sclEditor.GetSelections();

        var uri = (await _sclEditor.GetModel()).Uri;

        var edits = Formatter
            .FormatDocument(sclText, _stepFactoryStore)
            .ToList();

        await _sclEditor.ExecuteEdits(uri, edits, selections);
    }

    private async Task Run()
    {
        var sclText = await _sclEditor.GetValue();

        RunCancellation?.Cancel();
        var cts = new CancellationTokenSource();
        RunCancellation = cts;

        var loggerSink = _testLoggerFactory.CreateLogger("SCL");
        var logger     = new LoggyLog(loggerSink);
        logger.PropertyChanged += (_, _) => SetLogBadge(true);

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, _stepFactoryStore));

        _consoleStringBuilder.AppendLine("Sequence Running\n");
        SetOutputBadge(true);

        if (stepResult.IsFailure)
        {
            _consoleStringBuilder.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            var externalContext = new ExternalContext(
                ExternalProcessRunner.Instance,
                new BlazorRestClientFactory(HttpClientFactory),
                ConsoleAdapter.Instance,
                (ConnectorInjection.FileSystemKey, FileSystem.FileSystem),
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
                _consoleStringBuilder.AppendLine(runResult.Error.AsString);
            else if (runResult.Value is Unit)
                _consoleStringBuilder.AppendLine("\nSequence Completed Successfully");
            else
                _consoleStringBuilder.AppendLine(runResult.Value.ToString());
        }

        RunCancellation = null;

        _consoleStringBuilder.AppendLine();
        SetOutputBadge(true);
    }

    private static StandaloneEditorConstructionOptions SCLEditorConstructionOptions(MonacoEditor _)
    {
        return new()
        {
            AutomaticLayout = true,
            Language        = "scl",
            Value = @"- print 123
- log 456",
            Minimap = new EditorMinimapOptions { Enabled = false }
        };
    }

    private StandaloneEditorConstructionOptions GetFileEditorConstructionOptions(FileData file)
    {
        var extension =
            GetLanguageFromFileExtension(FileSystem.FileSystem.Path.GetExtension(file.Path));

        return new()
        {
            AutomaticLayout = true,
            Language        = extension,
            Value           = file.Data.TextContents,
            WordWrap        = "off",
            TabSize         = 8,
            UseTabStops     = true,
        };

        static string GetLanguageFromFileExtension(string? extension)
        {
            return extension?.ToLowerInvariant() switch

            {
                "yml"  => "yaml",
                "yaml" => "yaml",
                "json" => "json",
                "cs"   => "csharp",
                _      => ""
            };
        }
    }
}
