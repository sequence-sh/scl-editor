using MudBlazor;

namespace Reductech.Utilities.SCLEditor.Util;

/// <summary>
/// Razor component wrapping the Monaco editor
/// </summary>
public partial class Editor : IDisposable
{
    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Parameter] public string? Title { get; set; }

    [Parameter] public string? DefaultExtension { get; set; }

    [Parameter] public EditorConfiguration? Configuration { get; set; } = new();

    [Parameter] public RenderFragment? ConfigurationMenu { get; set; }

    [Parameter] public bool ConfigurationMenuEnabled { get; set; } = true;

    [Parameter] public bool ToolbarEnabled { get; set; } = true;

    /// <summary>
    /// Additional items to render in the toolbar
    /// </summary>
    [Parameter]
    public RenderFragment? Toolbar { get; set; }

    //[Parameter] public FileData? File { get; set; }

    public MonacoEditor Instance { get; private set; } = null!;

    /// <summary>
    /// The File System
    /// </summary>
    [Inject]
    public CompoundFileSystem? FileSystem { get; set; }

    protected bool HotChanges = false;

    private MudMessageBox SaveDialog { get; set; } = null!;

    private async Task SaveFile()
    {
        if (FileSystem is null)
            return;

        if (Title is null)
        {
            var change = await SaveDialog.Show(new DialogOptions());

            if (change != true)
                return;

            if (Title is null)
                return;
        }

        if (DefaultExtension is not null && !Title.EndsWith(
                DefaultExtension,
                StringComparison.InvariantCultureIgnoreCase
            ))
        {
            Title += DefaultExtension;
        }

        HotChanges = false;

        await FileSystem.SaveFile(Instance, Title);
    }

    private bool _isConfigPropChangeRegistered;

    protected override async Task OnInitializedAsync()
    {
        if (Configuration is not null)
        {
            if (FileSystem is not null)
            {
                var containsConfigKey =
                    await FileSystem.LocalStorage.ContainKeyAsync(Configuration.ConfigurationKey);

                if (containsConfigKey)
                    Configuration =
                        await FileSystem.LocalStorage.GetItemAsync<EditorConfiguration>(
                            Configuration.ConfigurationKey
                        );
            }

            Configuration.PropertyChanged += Configuration_PropertyChanged;
            _isConfigPropChangeRegistered =  true;
        }

        await base.OnInitializedAsync();
    }

    protected virtual void OnDidChangeModelContent() => HotChanges = true;

    private void Configuration_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        #pragma warning disable CS4014
        SaveConfiguration();
        #pragma warning restore CS4014
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
    }

    private async Task SaveConfiguration()
    {
        if (FileSystem is null)
            return;

        await FileSystem.LocalStorage.SetItemAsync(Configuration!.ConfigurationKey, Configuration);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isConfigPropChangeRegistered && Configuration is not null)
            Configuration.PropertyChanged -= Configuration_PropertyChanged;
    }

    protected virtual StandaloneEditorConstructionOptions ConstructionOptions(MonacoEditor _) =>
        new() { AutomaticLayout = true, Minimap = new EditorMinimapOptions { Enabled = false } };
}
