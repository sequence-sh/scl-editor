namespace Reductech.Utilities.SCLEditor.LanguageServer.Objects;

public class CompletionRequest : Request
{
    public CompletionTriggerKind CompletionTrigger { get; set; }

    public char? TriggerCharacter { get; set; }
}
