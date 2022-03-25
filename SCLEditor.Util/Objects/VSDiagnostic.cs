using Reductech.Sequence.Core.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.Util.Objects;

public record VSDiagnostic(
    string Message,
    int Severity,
    int StartLineNumber,
    int StartColumn,
    int EndLineNumber,
    int EndColumn)
{
    public VSDiagnostic(Diagnostic diagnostic) : this(
        diagnostic.Message,
        diagnostic.Severity,
        diagnostic.Start.Line + 1,
        diagnostic.Start.Character + 1,
        diagnostic.End.Line + 1,
        diagnostic.End.Character + 1
    ) { }
}
