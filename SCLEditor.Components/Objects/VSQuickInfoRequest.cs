namespace Sequence.SCLEditor.Components.Objects;

/// <summary>
/// A VS Code quick info request
/// </summary>
public record VSQuickInfoRequest(VSPosition Position) : VSRequest(Position);
