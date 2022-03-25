namespace Reductech.Utilities.SCLEditor.Util.Objects;

public record VSSignatureHelpRequest(VSPosition Position) : VSRequest(Position) { }
