using System.Reactive.Linq;
using System.Reactive.Subjects;
using MudBlazor;

namespace Reductech.Utilities.SCLEditor.Util;

/// <summary>
/// Playground for using SCL
/// </summary>
public sealed partial class Playground : IDisposable
{
    private const string DefaultEditorContent = @"- <list> = 
  (a: 1 b: 'two'),
  (a: 3 b: 'four'),
  (a: 5 c: 'six')

- <list> | EntityMapProperties To: (
    'ColA': 'a'
    'ColB': [ 'b', 'c' ]
  ) | ToCSV | Print

- <list> | ToJsonArray | Log
";

    private const string DefaultSCLExtension = ".scl";

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

    private MudDynamicTabs _editorTabsRef = null!;

    private readonly ITestLoggerFactory _testLoggerFactory =
        TestLoggerFactory.Create(x => x.FilterByMinimumLevel(LogLevel.Information));

    private readonly StringBuilder _consoleStringBuilder = new();

    private FileSelection _fileSelection = null!;

    private MudTextField<string> _outputTextField = null!;

    private MudTextField<string> _logTextField = null!;

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
        AddEditorTab();
    }

    private readonly Subject<bool> _disposed = new();

    public void Dispose()
    {
        _disposed.OnNext(true);
    }

#region EditorTabs

    private class EditorTab
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Title { get; set; } = "Untitled";

        public EditorConfiguration Configuration { get; init; } = new EditorConfigurationSCL();

        public string Extension { get; set; } = DefaultSCLExtension;

        public Editor Instance { get; set; } = null!;

        public SCLLanguageHelper? SCLHelper { get; set; }

        public FileData? File { get; set; }

        public Func<MonacoEditor, StandaloneEditorConstructionOptions> ConstructionOptions
        {
            get;
            set;
        } = _ => new()
        {
            AutomaticLayout = true,
            Language        = "scl",
            Minimap         = new EditorMinimapOptions { Enabled = false }
        };
    }

    private List<EditorTab> _editorTabs = new();
    private int _editorTabIndex = 0;
    private bool _updateEditorTabIndex = false;

    private void AddEditorTab() => AddEditorTab(null);

    private void AddEditorTab(FileData? file)
    {
        var languageHelper = new SCLLanguageHelper(Runtime, HttpClientFactory, _testLoggerFactory)
        {
            ConsoleStream       = _consoleStringBuilder,
            OnNewConsoleMessage = SetOutputBadge,
            OnNewLogMessage     = (_, _) => SetLogBadge(true),
            OnStateHasChanged   = StateHasChanged,
            OnRunComplete = async () =>
            {
                if (_outputTextField.InputReference?.ElementReference != null)
                    await Runtime.InvokeVoidAsync(
                        "scrollToEnd",
                        _outputTextField.InputReference.ElementReference
                    );

                if (_logTextField.InputReference?.ElementReference != null)
                    await Runtime.InvokeVoidAsync(
                        "scrollToEnd",
                        _logTextField.InputReference.ElementReference
                    );
            }
        };

        if (file is null)
        {
            if (_editorTabs.Count == 0)
                _editorTabs.Add(
                    new EditorTab
                    {
                        ConstructionOptions = _ => new()
                        {
                            AutomaticLayout = true,
                            Language        = "scl",
                            Value           = DefaultEditorContent,
                            Minimap         = new EditorMinimapOptions { Enabled = false }
                        },
                        SCLHelper = languageHelper
                    }
                );
            else
                _editorTabs.Add(new EditorTab { SCLHelper = languageHelper });
        }
        else
        {
            var extension = Path.GetExtension(file.Path);

            var isScl = extension.Equals(
                DefaultSCLExtension,
                StringComparison.InvariantCultureIgnoreCase
            );

            var config = isScl ? new EditorConfigurationSCL() : new EditorConfiguration();

            var language = GetLanguageFromFileExtension(extension);

            _editorTabs.Add(
                new EditorTab
                {
                    Title         = file.Path,
                    Configuration = config,
                    Extension     = extension,
                    File          = file,
                    ConstructionOptions = _ => new()
                    {
                        AutomaticLayout = true,
                        Language        = language,
                        TabSize         = new[] { "yaml", "json" }.Contains(language) ? 2 : 4,
                        Value           = file.Data.TextContents,
                        Minimap         = new EditorMinimapOptions { Enabled = false }
                    },
                    SCLHelper = isScl ? languageHelper : null
                }
            );
        }

        _updateEditorTabIndex = true;
        StateHasChanged();

        static string GetLanguageFromFileExtension(string? extension) =>
            extension?.ToLowerInvariant().TrimStart('.') switch
            {
                "yml"  => "yaml",
                "yaml" => "yaml",
                "json" => "json",
                "cs"   => "csharp",
                _      => ""
            };
    }

    private void CloseEditorTab(MudTabPanel panel)
    {
        var editorTab = _editorTabs.FirstOrDefault(x => x.Id == (string)panel.Tag);

        if (editorTab is not null)
            _editorTabs.Remove(editorTab);
    }

    private void RefreshTabTitle(EditorTab tab)
    {
        tab.Title = tab.Instance.Title!;
        StateHasChanged();
    }

#endregion EditorTabs

    protected override void OnAfterRender(bool firstRender)
    {
        if (_updateEditorTabIndex)
        {
            _editorTabIndex = _editorTabs.Count - 1;
            StateHasChanged();
            _updateEditorTabIndex = false;
        }
    }

    private void OpenFileAction(FileData file)
    {
        var alreadyOpen = _editorTabs.FirstOrDefault(t => t.Title.Equals(file.Path));

        if (alreadyOpen is null)
            AddEditorTab(file);
        else
            _editorTabsRef.ActivatePanel(alreadyOpen.Id);
    }

#region OutputTabs

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

#endregion OutputTabs
}
