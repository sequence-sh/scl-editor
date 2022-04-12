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

    private readonly ITestLoggerFactory _testLoggerFactory =
        TestLoggerFactory.Create(x => x.FilterByMinimumLevel(LogLevel.Information));

    private readonly StringBuilder _consoleStringBuilder = new();

    private EditorConfiguration _configuration = new();

    private SCLEditorConfiguration _sclConfiguration = new();

    private SCLEditor _sclEditorInstance = null!;

    private PropertyChangedEventHandler _onNewLogMessage = null!;

    private FileSelection _fileSelection = null!;

    private FileData? _openedFile = null;

    private async Task SetTheme(bool isDarkMode)
    {
        CurrentTheme = isDarkMode ? Themes.DarkTheme : Themes.DefaultTheme;
        var theme = isDarkMode ? "vs-dark" : "vs";

        await MonacoEditorBase.SetTheme(theme);

        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _onNewLogMessage = (_, _) => SetLogBadge(true);
    }

    private readonly Subject<bool> _disposed = new();

    public void Dispose()
    {
        _disposed.OnNext(true);
    }

    private async Task OpenFileAction(FileData arg)
    {
        if (Path.GetExtension(arg.Path) == ".scl")
        {
            //_title = arg.Path;
            //_hotChanges = false;
            await _sclEditor.SetValue(arg.Data.TextContents);
        }
        else
        {
            //CloseOpenFile();

            _openedFile = arg;

            if (_fileEditor is not null)
                await _fileEditor.SetValue(arg.Data.TextContents);

            StateHasChanged();
        }
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
        StateHasChanged();
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
        StateHasChanged();
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
        var text = string.Join("\n", _testLoggerFactory.Sink.LogEntries.Select(x => x.Message));
        return text;
    }
}
