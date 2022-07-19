using MudBlazor;

namespace Reductech.Utilities.SCLEditor.Components;

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
    public ILanguageHelper LanguageHelper { get; set; } = null!;

    /// <summary>
    /// The instance of the Monaco editor.
    /// Set by reference
    /// </summary>
    public MonacoEditor Instance { get; private set; } = null!;

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

    private delegate void ModelContentChangeEventHandler(ModelContentChangedEvent e);

    private event ModelContentChangeEventHandler? ModelContentChanged;

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

            await LanguageHelper.InitialSetup(
                new MonacoEditorWrapper(
                    Instance,
                    this.Configuration,
                    FileSystem?.FileSystem
                )
            );

            //    .OnInitializedAsync(
            //    new MonacoEditorWrapper(
            //        Instance,
            //        Configuration,
            //        FileSystem?.FileSystem
            //    )
            //);

            if (File is not null)
            {
                Title = File.Path;
                await Instance.SetValue(File.Data.TextContents);
            }
        }
    }

    protected virtual void OnDidChangeModelContent(ModelContentChangedEvent e)
    {
        LanguageHelper?.OnDidChangeModelContent();
        HotChanges = true;
        ModelContentChanged?.Invoke(e);
    }

    private void Configuration_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Instance is null)
            return;

        if (e.PropertyName == nameof(EditorConfiguration.MinimapEnabled))
        {
            Instance.UpdateOptions(
                new GlobalEditorOptions
                {
                    Minimap = new EditorMinimapOptions
                    {
                        Enabled = Configuration!.MinimapEnabled
                    }
                }
            );
        }
        else if (e.PropertyName == nameof(EditorConfiguration.ReadOnly))
        {
            Instance.UpdateOptions(new GlobalEditorOptions { ReadOnly = Configuration!.ReadOnly });
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isConfigPropChangeRegistered && Configuration is not null)
            Configuration.PropertyChanged -= Configuration_PropertyChanged;
    }

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

    public static int GetLanguageTabSize(string language) =>
        new[] { "yaml", "json" }.Contains(language) ? 2 : 4;
}
