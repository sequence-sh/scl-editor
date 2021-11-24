namespace Reductech.Utilities.SCLEditor.LanguageServer.Objects;

public record Diagnostic(LinePosition Start, LinePosition End, string Message, int Severity);
