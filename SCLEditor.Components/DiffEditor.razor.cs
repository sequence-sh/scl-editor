namespace Reductech.Utilities.SCLEditor.Components;

public partial class DiffEditor
{
    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Parameter] public string? Title { get; set; }

    public MonacoDiffEditor Instance { get; private set; } = null!;

    [Parameter] public bool HeaderEnabled { get; set; } = true;
    [Parameter] public bool ToolbarEnabled { get; set; } = true;

    [Parameter] public EditorConfiguration? Configuration { get; set; } = new();

    [Parameter] public RenderFragment? ConfigurationMenu { get; set; }

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
    } = (MonacoDiffEditor _) => new()
    {
        AutomaticLayout = true, Minimap = new EditorMinimapOptions { Enabled = false }
    };
}
