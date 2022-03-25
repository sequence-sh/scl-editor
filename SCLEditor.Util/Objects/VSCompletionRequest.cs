namespace Reductech.Utilities.SCLEditor.Util.Objects;

public record VSCompletionRequest(
    VSPosition Position,
    CompletionTriggerKind CompletionTrigger,
    char? TriggerCharacter) : VSRequest(Position);
