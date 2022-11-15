using MudBlazor;

namespace Sequence.SCLEditor.Components;

/// <summary>
/// Razor component wrapping the Monaco editor
/// </summary>
public partial class Editor : IDisposable
{
    /// <summary>
    /// Unique Id for this editor
    /// </summary>
    [Parameter]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Title for this editor
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// The File that is being edited
    /// </summary>
    [Parameter]
    public FileData? File { get; set; }

    /// <summary>
    /// Default extension for new files
    /// </summary>
    [Parameter]
    public string? DefaultExtension { get; set; }

    /// <summary>
    /// Editor construction options
    /// </summary>
    [Parameter]
    public virtual Func<MonacoEditor, StandaloneEditorConstructionOptions>
        ConstructionOptions { get; set; } = _ => new()
    {
        AutomaticLayout = true, Minimap = new EditorMinimapOptions { Enabled = false }
    };

    /// <summary>
    /// The configuration for this editor
    /// </summary>
    [Parameter]
    public EditorConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// The configuration menu
    /// </summary>
    [Parameter]
    public RenderFragment? ConfigurationMenu { get; set; }

    /// <summary>
    /// Is the header enabled
    /// </summary>
    [Parameter]
    public bool HeaderEnabled { get; set; } = true;

    /// <summary>
    /// Is the toolbar enabled
    /// </summary>
    [Parameter]
    public bool ToolbarEnabled { get; set; } = true;

    /// <summary>
    /// Additional items to render in the toolbar
    /// </summary>
    [Parameter]
    public RenderFragment? Toolbar { get; set; }

    /// <summary>
    /// Called when a file is saved
    /// </summary>
    [Parameter]
    public Action? OnFileSave { get; set; }

    /// <summary>
    /// The languageHelper
    /// </summary>
    [EditorRequired]
    [Parameter]
    #pragma warning disable CS8618
    public ILanguageHelper LanguageHelper { get; set; }

    /// <summary>
    /// The instance of the Monaco editor.
    /// Set by reference
    /// </summary>
    public MonacoEditor Instance { get; private set; }
    #pragma warning restore CS8618
    /// <summary>
    /// The File System
    /// </summary>
    [Parameter]
    public CompoundFileSystem? FileSystem { get; set; }

    /// <summary>
    /// Whether there are hot changed
    /// </summary>
    protected bool HotChanges;

    private MudMessageBox SaveDialog { get; set; } = null!;

    /// <summary>
    /// Called when the content of the model changes
    /// </summary>
    [Parameter]
    public Action<ModelContentChangedEvent>? OnModelContentChanged { get; set; }

    /// <summary>
    /// Save the edited file
    /// </summary>
    public async Task SaveFile()
    {
        if (FileSystem is null)
            return;

        if (File is not null)
        {
            await FileSystem.SaveFile(Instance, File.Path);
        }
        else
        {
            var change = await SaveDialog.Show(new DialogOptions());

            if (change != true)
                return;

            if (Title is null)
                return;

            if (DefaultExtension is not null && !Title.EndsWith(
                    DefaultExtension,
                    StringComparison.InvariantCultureIgnoreCase
                ))
            {
                Title += DefaultExtension;
            }

            File = await FileSystem.SaveFile(Instance, Title);
        }

        HotChanges = false;

        OnFileSave?.Invoke();
    }

    private bool _isConfigPropChangeRegistered;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            Configuration.PropertyChanged += Configuration_PropertyChanged;
            _isConfigPropChangeRegistered =  true;
        }

        if (LanguageHelper is not null)
            await LanguageHelper.InitialSetup(
                new MonacoEditorWrapper(
                    Instance,
                    this.Configuration,
                    FileSystem?.FileSystem
                )
            );
    }

    /// <summary>
    /// Is called when the model content is changed
    /// </summary>
    protected virtual void OnDidChangeModelContent(ModelContentChangedEvent e)
    {
        LanguageHelper.OnDidChangeModelContent();
        HotChanges = true;
        OnModelContentChanged?.Invoke(e);
    }

    private void Configuration_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorConfiguration.MinimapEnabled)
            or nameof(EditorConfiguration.ReadOnly))
        {
            Instance.UpdateOptions(Configuration.ToGlobalEditorOptions());
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_isConfigPropChangeRegistered)
            Configuration.PropertyChanged -= Configuration_PropertyChanged;
    }

    /// <summary>
    /// Gets the language name from a file extension
    /// </summary>
    public static string GetLanguageFromFileExtension(string? extension) =>
        extension?.ToLowerInvariant().TrimStart('.') switch
        {
            "yml"  => "yaml",
            "yaml" => "yaml",
            "json" => "json",
            "cs"   => "csharp",
            "scl"  => "scl",
            _      => ""
        };

    /// <summary>
    /// Get the tab size to use for various languages
    /// </summary>
    public static int GetLanguageTabSize(string language) =>
        new[] { "yaml", "json" }.Contains(language) ? 2 : 4;
}
