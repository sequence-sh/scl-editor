using System.Reactive.Subjects;
using MudBlazor;

namespace Reductech.Utilities.SCLEditor.Util;

/// <summary>
/// Razor component wrapping the Monaco editor
/// </summary>
public partial class Editor : IDisposable
{
    //[Parameter] public StandaloneEditorConstructionOptions Options { get; set; } = null!;

    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Parameter] public string? Title { get; set; }

    [Parameter] public string? DefaultExtension { get; set; }

    [Parameter] public RenderFragment? ToolBar { get; set; }

    [Parameter] public RenderFragment? SettingsMenu { get; set; }

    //[Parameter] public FileData? File { get; set; }

    [Parameter] public EventCallback<ModelContentChangedEvent> OnDidChangeModelContent { get; set; }

    public MonacoEditor Instance { get; private set; } = null!;

    protected bool HotChanges = false;

    /// <summary>
    /// The File System
    /// </summary>
    [Inject]
    public CompoundFileSystem FileSystem { get; set; } = null!;

    private MudMessageBox SaveDialog { get; set; } = null!;

    private async Task SaveFile()
    {
        if (Title is null)
        {
            var change = await SaveDialog.Show(new DialogOptions());

            if (change != true)
                return;

            if (Title is null)
                return;
        }

        if (DefaultExtension is not null && !Title.EndsWith(
                ".scl",
                StringComparison.InvariantCultureIgnoreCase
            ))
        {
            Title += DefaultExtension;
        }

        HotChanges = false;

        await FileSystem.SaveFile(Instance, Title);
    }

    /// <inheritdoc />
    //protected override async Task OnAfterRenderAsync(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //        var containsConfigKey =
    //            await FileSystem.LocalStorage.ContainKeyAsync(EditorConfiguration.ConfigurationKey);

    //        if (containsConfigKey)
    //            _configuration = await
    //                FileSystem.LocalStorage.GetItemAsync<EditorConfiguration>(
    //                    EditorConfiguration.ConfigurationKey
    //                );
    //        else
    //            _configuration = new EditorConfiguration();

    //        _configuration.PropertyChanged += Configuration_PropertyChanged;
    //    }

    //    await base.OnAfterRenderAsync(firstRender);
    //}
    private readonly Subject<bool> _disposed = new();

    public void Dispose() => _disposed.OnNext(true);

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
}
