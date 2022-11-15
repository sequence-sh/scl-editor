namespace Sequence.SCLEditor.Components;

/// <summary>
/// A diff Editor component
/// </summary>
public partial class DiffEditor : IDisposable
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
    /// The languageHelper
    /// </summary>
    [EditorRequired]
    [Parameter]
    #pragma warning disable CS8618
    public ILanguageHelper OriginalLanguageHelper { get; set; }

    /// <summary>
    /// The languageHelper
    /// </summary>
    [EditorRequired]
    [Parameter]
    public ILanguageHelper ModifiedLanguageHelper { get; set; }

    /// <summary>
    /// The editor instance. Set by reference
    /// </summary>
    public MonacoDiffEditor Instance { get; private set; }
    #pragma warning restore CS8618

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
    /// Additional items to render in the toolbar
    /// </summary>
    [Parameter]
    public RenderFragment? Toolbar { get; set; }

    /// <summary>
    /// Construction Options for the Diff Editor
    /// </summary>
    [Parameter]
    public virtual Func<MonacoDiffEditor, DiffEditorConstructionOptions>
        ConstructionOptions
    {
        get;
        set;
    } = _ => new()
    {
        AutomaticLayout = true, Minimap = new EditorMinimapOptions { Enabled = false }
    };

    [Parameter] public string? InitialValueOriginal { get; set; }

    [Parameter] public string? InitialValueModified { get; set; }

    private bool _isConfigPropChangeRegistered;

    private async Task EditorOnDidInit(MonacoEditorBase obj)
    {
        // Get or create the original model
        var original_model = await MonacoEditorBase.GetModel($"{Id}-originalModel");

        if (original_model == null)
        {
            original_model = await MonacoEditorBase.CreateModel(
                InitialValueOriginal,
                "javascript",
                $"{Id}-originalModel"
            );
        }

        // Get or create the modified model
        var modified_model = await MonacoEditorBase.GetModel($"{Id}-modifiedModel");

        if (modified_model == null)
        {
            modified_model = await MonacoEditorBase.CreateModel(
                InitialValueModified,
                "javascript",
                $"{Id}-modifiedModel"
            );
        }

        // Set the editor model
        await Instance.SetModel(
            new DiffEditorModel { Original = original_model, Modified = modified_model }
        );

        await OriginalLanguageHelper.InitialSetup(
            new DiffEditorWrapper(Instance, Configuration, null, true)
        );

        await ModifiedLanguageHelper.InitialSetup(
            new DiffEditorWrapper(Instance, Configuration, null, false)
        );
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            Configuration.PropertyChanged += Configuration_PropertyChanged;
            _isConfigPropChangeRegistered =  true;
        }
    }

    private void Configuration_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorConfiguration.MinimapEnabled))
        {
            Instance.OriginalEditor.UpdateOptions(
                new GlobalEditorOptions
                {
                    Minimap = new EditorMinimapOptions
                    {
                        Enabled = Configuration.MinimapEnabled
                    }
                }
            );

            Instance.ModifiedEditor.UpdateOptions(
                new GlobalEditorOptions
                {
                    Minimap = new EditorMinimapOptions
                    {
                        Enabled = Configuration.MinimapEnabled
                    }
                }
            );
        }
        else if (e.PropertyName == nameof(EditorConfiguration.ReadOnly))
        {
            Instance.OriginalEditor.UpdateOptions(
                new GlobalEditorOptions { ReadOnly = Configuration.ReadOnly }
            );

            Instance.ModifiedEditor.UpdateOptions(
                new GlobalEditorOptions { ReadOnly = Configuration.ReadOnly }
            );
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_isConfigPropChangeRegistered)
            Configuration.PropertyChanged -= Configuration_PropertyChanged;
    }
}
