using MudBlazor;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// Razor component wrapping the Monaco editor
/// </summary>
public partial class Editor : IDisposable
{
    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Parameter] public string? Title { get; set; }

    [Parameter] public FileData? File { get; set; }

    [Parameter] public string? DefaultExtension { get; set; }

    [Parameter]
    public virtual Func<MonacoEditor, StandaloneEditorConstructionOptions>
        ConstructionOptions { get; set; } = (MonacoEditor _) => new()
    {
        AutomaticLayout = true, Minimap = new EditorMinimapOptions { Enabled = false }
    };

    [Parameter] public EditorConfiguration? Configuration { get; set; } = new();

    [Parameter] public RenderFragment? ConfigurationMenu { get; set; }

    [Parameter] public bool HeaderEnabled { get; set; } = true;

    [Parameter] public bool ToolbarEnabled { get; set; } = true;

    /// <summary>
    /// Additional items to render in the toolbar
    /// </summary>
    [Parameter]
    public RenderFragment? Toolbar { get; set; }

    [Parameter] public Action? OnFileSave { get; set; }

    [Parameter] public ILanguageHelper? LanguageHelper { get; set; }

    public MonacoEditor Instance { get; private set; } = null!;

    /// <summary>
    /// The File System
    /// </summary>
    [Parameter]
    public CompoundFileSystem? FileSystem { get; set; }

    protected bool HotChanges = false;

    private MudMessageBox SaveDialog { get; set; } = null!;

    public delegate void ModelContentChangeEventHandler(ModelContentChangedEvent e);

    public event ModelContentChangeEventHandler? ModelContentChanged;

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

    protected override async Task OnInitializedAsync()
    {
        if (Configuration is not null)
        {
            Configuration.PropertyChanged += Configuration_PropertyChanged;
            _isConfigPropChangeRegistered =  true;
        }

        if (LanguageHelper is not null)
            await LanguageHelper.OnInitializedAsync(this);

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (LanguageHelper is not null)
        {
            await LanguageHelper.OnAfterRenderAsync(firstRender);
        }

        if (firstRender)
        {
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
