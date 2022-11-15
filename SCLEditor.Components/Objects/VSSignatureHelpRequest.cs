namespace Sequence.SCLEditor.Components.Objects;

/// <summary>
/// A Visual Studio Code Signature help request
/// </summary>
public record VSSignatureHelpRequest(VSPosition Position) : VSRequest(Position);
