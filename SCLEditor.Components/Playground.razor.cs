using System.Reactive.Linq;
using System.Reactive.Subjects;
using MudBlazor;
using Reductech.Sequence.Connectors.FileSystem.Steps;
using Reductech.Sequence.Connectors.StructuredData;
using Reductech.Sequence.Core.Abstractions;
using Reductech.Sequence.Core.Internal;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// Playground for using SCL
/// </summary>
public sealed partial class Playground : IDisposable
{
    private const string DefaultEditorContent = @"# This is a comment, it's ignored when running SCL
# If you get stuck when using the Playground, use Ctrl + Space for a hint

# Steps in SCL start with a dash '-' or a pipeline '|'. Just like bullet
# points in a list or arrays in YAML:
- <sum> = 1 + 1
- Print <sum>

# Variables in SCL are defined using angle brackets <>
# This is a variable, a data placeholder:
- <greeting> = 'hello'

# Steps are 'units of work' or actions in an application
# This is a step that will make the value of <greeting> upper case
- <greeting> = ChangeCase of: <greeting> to: 'Upper'

# Here is a step that will print the value to the 'Output' tab:
- Print <greeting>

# And this is a step that will log the value to the 'Log' tab.
# Logging in Sequence is highly configurable, instead of the 'Log' tab
# it could go to a file, a database, an Elastic instance, etc.
- Log <greeting>

# Data in SCL is represented as Entities which can be defined using round brackets ().
# An entity is a collection of property names and values, or key-value pairs. The
# property name precedes a colon ':' and the property value follows - name: 'value'.
- <entity> = (name: 'value1' key: 'value2')

# To get a property value from an entity, use the EntityGetValue step:
- <value> = EntityGetValue Entity: <entity> Property: 'name'
- Print <value> # will print 'value1'

# EntityGetValue also has a shorthand notation - the period '.'
- Print <entity>.name

# Lists (or Arrays as we call them) in SCL can be defined using a comma, or
# square brackets []. Here is a list of entities:
- <list> = (a: 1 b: 'two'), (a: 3 b: 'four'), (a: 5 c: 'six')

# To access items in a list, use ArrayElementAtIndex step, or the square bracket
# shorthand []:
- Print ArrayElementAtIndex <list> 2 # prints (a: 3 b: 'four')
- Print <list>[2] # does exactly the same and prints (a: 3 b: 'four')

# Many steps are available to work with entities and arrays.
- Print EntityGetProperties <entity> # will print out a list of properties: ['name', 'key']
- Print ArrayLength <list> # will print out 3

# There are also many steps available to work with text/strings. An example:
- Print StringReplace In: 'A long sentence' Find: 'long' Replace: 'short'

# The pipe character | is used to chain steps into sequences. The output of the
# previous step is used as the input for the next. Here is a sequence of steps
# that takes the list of entites, converts into JSON and writes it to a file:
- <list> | ToJsonArray | FileWrite Path: 'my-list.json'

# Sequence is designed to be extensible via connectors. The playground has the
# File System and Structured Data connectors installed (many others are available).
# The Strutured Data connector has steps to convert entities To/From various data
# formats. Here is a sequence of steps that reads the json file, converts it to
# entities, maps the properties to different names, and export to a CSV file:
- <myfile> = 'my-list.json'
- <mynewfile> = StringReplace In: <myfile> Find: 'json' Replace: 'csv'
- FileRead <myfile> | FromJsonArray | EntityMapProperties To: (
    'ColA': 'a'          # Property 'a' will be renamed to 'ColA'
    'ColB': [ 'b', 'c' ] # Both 'b' and 'c' get renamed to 'ColB'
  ) | ToCSV | FileWrite <mynewfile>

# Steps and parameters in SCL can have one or more aliases. Above, 'FileRead' is
# used, but here is the same step, but using its alias 'ReadFromFile':
- ReadFromFile <mynewfile> | Print
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

    private MudDynamicTabs _editorTabsRef = null!;

    private readonly ITestLoggerFactory _testLoggerFactory =
        TestLoggerFactory.Create(x => x.FilterByMinimumLevel(LogLevel.Information));

    private readonly StringBuilder _consoleStringBuilder = new();

    private FileSelection _fileSelection = null!;

    private MudTextField<string> _outputTextField = null!;

    private MudTextField<string> _logTextField = null!;

    private async Task SetTheme(bool isDarkMode)
    {
        var theme = isDarkMode ? "vs-dark" : "vs";
        await MonacoEditorBase.SetTheme(theme);
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        AddEditorTab();
    }

    private readonly Subject<bool> _disposed = new();

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed.OnNext(true);
    }

#region EditorTabs

    private class EditorTab
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Title { get; set; } = "Untitled";

        public EditorConfiguration Configuration { get; init; } = new();

        public string Extension { get; set; } = DefaultSCLExtension;

        public Editor Instance { get; set; } = null!;

        public RunnableSCLLanguageHelper? SCLHelper { get; set; }

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

    private readonly List<EditorTab> _editorTabs = new();
    private int _editorTabIndex = 0;
    private bool _updateEditorTabIndex = false;

    private void AddEditorTab() => AddEditorTab(null);

    private static Task<StepFactoryStore> CreateStepFactoryStore()
    {
        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            ExternalContext.Default,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        return Task.FromResult(stepFactoryStoreResult.Value);
    }

    private void AddEditorTab(FileData? file)
    {
        var languageHelper = new RunnableSCLLanguageHelper(
            Runtime,
            HttpClientFactory,
            _testLoggerFactory,
            CreateStepFactoryStore,
            _consoleStringBuilder
        );

        languageHelper.OnNewConsoleMessage += (_, _) => SetOutputBadge(true);
        languageHelper.OnLogMessage        += (_, _) => SetLogBadge(true);
        languageHelper.OnStateChanged      += (_, _) => StateHasChanged();

        languageHelper.OnRunComplete += (_, _) =>
        {
            if (_outputTextField.InputReference?.ElementReference != null)
                Runtime.InvokeVoidAsync(
                        "scrollToEnd",
                        _outputTextField.InputReference.ElementReference
                    )
                    .AsTask();

            if (_logTextField.InputReference?.ElementReference != null)
                Runtime.InvokeVoidAsync(
                        "scrollToEnd",
                        _logTextField.InputReference.ElementReference
                    )
                    .AsTask();
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

            var config =
                new EditorConfiguration(); // isScl ? new EditorConfigurationSCL() : new EditorConfiguration();

            var language = Editor.GetLanguageFromFileExtension(extension);

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
                        TabSize         = Editor.GetLanguageTabSize(language),
                        Value           = file.Data.TextContents,
                        Minimap         = new EditorMinimapOptions { Enabled = false }
                    },
                    SCLHelper = isScl ? languageHelper : null
                }
            );
        }

        _updateEditorTabIndex = true;
        StateHasChanged();
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

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (_updateEditorTabIndex)
        {
            _editorTabIndex = _editorTabs.Count - 1;
            StateHasChanged();
            _updateEditorTabIndex = false;
        }

        if (firstRender)
        {
            Themes.IsDarkMode
                .TakeUntil(_disposed)
                .Select(x => Observable.FromAsync(() => SetTheme(x)))
                .Switch()
                .Subscribe();
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

    /// <summary>
    /// The index of the active output tab
    /// </summary>
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
