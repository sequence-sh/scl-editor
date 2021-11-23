using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.Utilities.SCLEditor.LanguageServer;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Reductech.Utilities.SCLEditor.Blazor
{

public class CompletionRequest : Request
{
    public CompletionTriggerKind CompletionTrigger { get; set; }

    public char? TriggerCharacter { get; set; }
}

public class LinePositionSpanTextChange
{
    public string NewText { get; set; } = "";

    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int StartLine { get; set; }

    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int StartColumn { get; set; }

    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int EndLine { get; set; }

    [JsonConverter(typeof(ZeroBasedIndexConverter))]
    public int EndColumn { get; set; }

    public override bool Equals(object obj) =>
        obj is LinePositionSpanTextChange positionSpanTextChange
     && NewText == positionSpanTextChange.NewText && StartLine == positionSpanTextChange.StartLine
     && StartColumn == positionSpanTextChange.StartColumn
     && EndLine == positionSpanTextChange.EndLine && EndColumn == positionSpanTextChange.EndColumn;

    public override int GetHashCode() => NewText.GetHashCode() * (23 + StartLine)
                                                               * (29 + StartColumn) * (31 + EndLine)
                                                               * (37 + EndColumn);

    public override string ToString() =>
        $"StartLine={StartLine}, StartColumn={StartColumn}, EndLine={EndLine}, EndColumn={EndColumn}, NewText='{(NewText.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t"))}'";
}

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

public class SimpleFileRequest
{
    private string _fileName;

    public string FileName
    {
        get => _fileName?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        set => _fileName = value;
    }
}

public enum CompletionTriggerKind
{
    Invoked = 1,
    TriggerCharacter = 2,

    [EditorBrowsable(EditorBrowsableState.Never)]
    TriggerForIncompleteCompletions = 3,
}

public class CompletionResponse
{
    public bool IsIncomplete { get; set; }

    public IReadOnlyList<CompletionItem> Items { get; set; }
}

public class CompletionItem
{
    public string Label { get; set; }

    public CompletionItemKind Kind { get; set; }

    public IReadOnlyList<CompletionItemTag>? Tags { get; set; }

    public string? Detail { get; set; }

    public string? Documentation { get; set; }

    public bool Preselect { get; set; }

    public string? SortText { get; set; }

    public string? FilterText { get; set; }

    public InsertTextFormat? InsertTextFormat { get; set; }

    public LinePositionSpanTextChange? TextEdit { get; set; }

    public IReadOnlyList<char>? CommitCharacters { get; set; }

    public IReadOnlyList<LinePositionSpanTextChange>? AdditionalTextEdits { get; set; }

    public int Data { get; set; }

    public override string ToString() => $"{{ Label = {Label}, CompletionItemKind = {Kind} }}";

    public static CompletionItem Create(
        OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem ci)
    {
        return new CompletionItem()
        {
            Label            = ci.Label,
            Kind             = (CompletionItemKind)(int)ci.Kind,
            Detail           = ci.Detail,
            Documentation    = ci.Documentation?.MarkupContent?.Value ?? ci.Documentation?.String,
            Preselect        = ci.Preselect,
            SortText         = ci.SortText,
            FilterText       = ci.FilterText,
            InsertTextFormat = (InsertTextFormat)(int)ci.InsertTextFormat,
            TextEdit         = Convert(ci.TextEdit),
            CommitCharacters = ci.CommitCharacters?.Select(x => x.First()).ToList(),
            AdditionalTextEdits = ci.AdditionalTextEdits
                ?.Select(x => Convert(x.NewText, x.Range))
                .ToList()
        };
    }

    private static LinePositionSpanTextChange? Convert(TextEditOrInsertReplaceEdit? edit)
    {
        if (edit is null)
            return null;

        if (edit.IsTextEdit)
            return Convert(edit.TextEdit!.NewText, edit.TextEdit.Range);

        if (edit.IsInsertReplaceEdit)
        {
            return Convert(
                edit.InsertReplaceEdit?.NewText ?? "",
                edit.InsertReplaceEdit?.Insert
             ?? edit.InsertReplaceEdit?.Replace ?? new Range(0, 0, 0, 0)
            );
        }

        return null;
    }

    private static LinePositionSpanTextChange Convert(string text, Range range)
    {
        return new LinePositionSpanTextChange()
        {
            NewText     = text,
            StartColumn = range.Start.Character,
            StartLine   = range.Start.Line,
            EndColumn   = range.End.Character,
            EndLine     = range.End.Line
        };
    }
}

public enum CompletionItemTag
{
    Deprecated = 1,
}

public enum CompletionItemKind
{
    Text = 1,
    Method = 2,
    Function = 3,
    Constructor = 4,
    Field = 5,
    Variable = 6,
    Class = 7,
    Interface = 8,
    Module = 9,
    Property = 10,      // 0x0000000A
    Unit = 11,          // 0x0000000B
    Value = 12,         // 0x0000000C
    Enum = 13,          // 0x0000000D
    Keyword = 14,       // 0x0000000E
    Snippet = 15,       // 0x0000000F
    Color = 16,         // 0x00000010
    File = 17,          // 0x00000011
    Reference = 18,     // 0x00000012
    Folder = 19,        // 0x00000013
    EnumMember = 20,    // 0x00000014
    Constant = 21,      // 0x00000015
    Struct = 22,        // 0x00000016
    Event = 23,         // 0x00000017
    Operator = 24,      // 0x00000018
    TypeParameter = 25, // 0x00000019
}

public enum InsertTextFormat
{
    PlainText = 1,
    Snippet = 2,
}

public class CompletionResolveRequest
{
    public CompletionItem Item { get; set; }
}

public class CompletionResolveResponse
{
    public CompletionItem? Item { get; set; }
}

public class SCLHelper
{
    public SCLHelper(StepFactoryStore stepFactoryStore)
    {
        StepFactoryStore = stepFactoryStore;
    }

    public StepFactoryStore StepFactoryStore { get; }

    [JSInvokable]
    public async Task<CompletionResponse> GetCompletionAsync(
        string code,
        CompletionRequest completionRequest)
    {
        var position = new Position(completionRequest.Line, completionRequest.Column);
        var visitor  = new CompletionVisitor(position, StepFactoryStore);

        var completionList = visitor.LexParseAndVisit(
            code,
            x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); }
        ) ?? new CompletionList();

        return new CompletionResponse
        {
            IsIncomplete = completionList.IsIncomplete,
            Items        = completionList.Items.Select(CompletionItem.Create).ToList()
        };
    }

    [JSInvokable]
    public async Task<CompletionResolveResponse> GetCompletionResolveAsync(
        CompletionResolveRequest completionResolveRequest)
    {
        return new CompletionResolveResponse { Item = completionResolveRequest.Item };
    }
}

}
