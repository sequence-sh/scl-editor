using Newtonsoft.Json;

namespace Reductech.Utilities.SCLEditor.LanguageServer.Objects;

public class Request : SimpleFileRequest
{
    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int Line { get; set; }

    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int Column { get; set; }

    public string Buffer { get; set; }

    public IEnumerable<LinePositionSpanTextChange> Changes { get; set; }

    public bool ApplyChangesTogether { get; set; }
}
