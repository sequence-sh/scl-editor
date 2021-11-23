using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CSharpFunctionalExtensions;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;
using Reductech.Utilities.SCLEditor.LanguageServer;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Reductech.Utilities.SCLEditor.Blazor;

public class CompletionRequest : Request
{
    public CompletionTriggerKind CompletionTrigger { get; set; }

    public char? TriggerCharacter { get; set; }
}

public readonly record struct LinePosition(int Line, int Character)
{
    public static LinePosition Zero => new();
}

public record Diagnostic(LinePosition Start, LinePosition End, string Message, int Severity);

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

public class SignatureHelpRequest : Request { }

public class SignatureHelpResponse
{
    public IEnumerable<SignatureHelpItem> Signatures { get; set; }

    public int ActiveSignature { get; set; }

    public int ActiveParameter { get; set; }
}

public class SignatureHelpParameter
{
    public string Name { get; set; }

    public string Label { get; set; }

    public string Documentation { get; set; }

    public override bool Equals(object? obj) => obj is SignatureHelpParameter signatureHelpParameter
                                             && Name == signatureHelpParameter.Name
                                             && Label == signatureHelpParameter.Label
                                             && Documentation
                                             == signatureHelpParameter.Documentation;

    public override int GetHashCode() => 17 * Name.GetHashCode()
                                       + 23 * Label.GetHashCode()
                                       + 31 * Documentation.GetHashCode();
}

public class DocumentationItem
{
    public string Name { get; }

    public string Documentation { get; }

    public DocumentationItem(string name, string documentation)
    {
        Name          = name;
        Documentation = documentation;
    }
}

public class DocumentationComment
{
    public static readonly DocumentationComment Empty = new();

    public string SummaryText { get; }

    public DocumentationItem[] TypeParamElements { get; }

    public DocumentationItem[] ParamElements { get; }

    public string ReturnsText { get; }

    public string RemarksText { get; }

    public string ExampleText { get; }

    public string ValueText { get; }

    public DocumentationItem[] Exception { get; }

    internal class DocumentationItemBuilder
    {
        public string Name { get; set; }

        public StringBuilder Documentation { get; set; }

        public DocumentationItemBuilder() => Documentation = new StringBuilder();

        public DocumentationItem ConvertToDocumentedObject() => new(Name, Documentation.ToString());
    }

    public DocumentationComment(
        string summaryText = "",
        DocumentationItem[]? typeParamElements = null,
        DocumentationItem[]? paramElements = null,
        string returnsText = "",
        string remarksText = "",
        string exampleText = "",
        string valueText = "",
        DocumentationItem[]? exception = null)
    {
        SummaryText       = summaryText;
        TypeParamElements = typeParamElements ?? Array.Empty<DocumentationItem>();
        ParamElements     = paramElements ?? Array.Empty<DocumentationItem>();
        ReturnsText       = returnsText;
        RemarksText       = remarksText;
        ExampleText       = exampleText;
        ValueText         = valueText;
        Exception         = exception ?? Array.Empty<DocumentationItem>();
    }

    public static DocumentationComment From(
        string xmlDocumentation,
        string lineEnding)
    {
        if (string.IsNullOrEmpty(xmlDocumentation))
            return Empty;

        var input          = new StringReader("<docroot>" + xmlDocumentation + "</docroot>");
        var stringBuilder1 = new StringBuilder();
        var source1        = new List<DocumentationItemBuilder>();
        var source2        = new List<DocumentationItemBuilder>();
        var stringBuilder2 = new StringBuilder();
        var stringBuilder3 = new StringBuilder();
        var stringBuilder4 = new StringBuilder();
        var stringBuilder5 = new StringBuilder();
        var source3        = new List<DocumentationItemBuilder>();

        using (var xmlReader = XmlReader.Create(input))
        {
            try
            {
                xmlReader.Read();
                string        str            = null;
                StringBuilder stringBuilder6 = null;

                do
                {
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        str = xmlReader.Name.ToLowerInvariant();

                        switch (str)
                        {
                            case "br":
                            case "para":
                                stringBuilder6.Append(lineEnding);
                                break;
                            case "example":
                                stringBuilder6 = stringBuilder4;
                                break;
                            case "exception":
                                var documentationItemBuilder1 = new DocumentationItemBuilder();

                                documentationItemBuilder1.Name =
                                    GetCref(xmlReader["cref"]).TrimEnd();

                                stringBuilder6 = documentationItemBuilder1.Documentation;
                                source3.Add(documentationItemBuilder1);
                                break;
                            case "filterpriority":
                                xmlReader.Skip();
                                break;
                            case "param":
                                var documentationItemBuilder2 = new DocumentationItemBuilder();

                                documentationItemBuilder2.Name = TrimMultiLineString(
                                    xmlReader["name"],
                                    lineEnding
                                );

                                stringBuilder6 = documentationItemBuilder2.Documentation;
                                source2.Add(documentationItemBuilder2);
                                break;
                            case "paramref":
                                stringBuilder6.Append(xmlReader["name"]);
                                stringBuilder6.Append(" ");
                                break;
                            case "remarks":
                                stringBuilder6 = stringBuilder3;
                                break;
                            case "returns":
                                stringBuilder6 = stringBuilder2;
                                break;
                            case "see":
                                stringBuilder6.Append(GetCref(xmlReader["cref"]));
                                stringBuilder6.Append(xmlReader["langword"]);
                                break;
                            case "seealso":
                                stringBuilder6.Append("See also: ");
                                stringBuilder6.Append(GetCref(xmlReader["cref"]));
                                break;
                            case "summary":
                                stringBuilder6 = stringBuilder1;
                                break;
                            case "typeparam":
                                var documentationItemBuilder3 = new DocumentationItemBuilder();

                                documentationItemBuilder3.Name = TrimMultiLineString(
                                    xmlReader["name"],
                                    lineEnding
                                );

                                stringBuilder6 = documentationItemBuilder3.Documentation;
                                source1.Add(documentationItemBuilder3);
                                break;
                            case "typeparamref":
                                stringBuilder6.Append(xmlReader["name"]);
                                stringBuilder6.Append(" ");
                                break;
                            case "value":
                                stringBuilder6 = stringBuilder5;
                                break;
                        }
                    }
                    else if (xmlReader.NodeType == XmlNodeType.Text && stringBuilder6 != null)
                    {
                        if (str == "code")
                            stringBuilder6.Append(xmlReader.Value);
                        else
                            stringBuilder6.Append(TrimMultiLineString(xmlReader.Value, lineEnding));
                    }
                } while (xmlReader.Read());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        return new DocumentationComment(
            stringBuilder1.ToString(),
            source1.Select(s => s.ConvertToDocumentedObject()).ToArray(),
            source2.Select(s => s.ConvertToDocumentedObject()).ToArray(),
            stringBuilder2.ToString(),
            stringBuilder3.ToString(),
            stringBuilder4.ToString(),
            stringBuilder5.ToString(),
            source3.Select(s => s.ConvertToDocumentedObject()).ToArray()
        );
    }

    private static string TrimMultiLineString(string input, string lineEnding)
    {
        var source = input.Split(
            new string[2] { "\n", "\r\n" },
            StringSplitOptions.RemoveEmptyEntries
        );

        return string.Join(lineEnding, source.Select(TrimStartRetainingSingleLeadingSpace));
    }

    private static string GetCref(string? cref)
    {
        if (cref == null || cref.Trim().Length == 0)
            return "";

        if (cref.Length < 2)
            return cref;

        return cref.Substring(1, 1) == ":" ? cref.Substring(2, cref.Length - 2) + " " : cref + " ";
    }

    private static string TrimStartRetainingSingleLeadingSpace(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return !char.IsWhiteSpace(input[0]) ? input : " " + input.TrimStart();
    }

    public string GetParameterText(string name) =>
        Array.Find(ParamElements, parameter => parameter.Name == name)?.Documentation
     ?? string.Empty;

    public string GetTypeParameterText(string name) =>
        Array.Find(TypeParamElements, typeParam => typeParam.Name == name)?.Documentation
     ?? string.Empty;
}

public class SignatureHelpItem
{
    public string Name { get; set; }

    public string Label { get; set; }

    public string Documentation { get; set; }

    public IEnumerable<SignatureHelpParameter> Parameters { get; set; }

    public DocumentationComment StructuredDocumentation { get; set; }

    public override bool Equals(object obj) => obj is SignatureHelpItem signatureHelpItem
                                            && Name == signatureHelpItem.Name
                                            && Label == signatureHelpItem.Label
                                            && Documentation == signatureHelpItem.Documentation
                                            && Parameters
                                                   .SequenceEqual(signatureHelpItem.Parameters);

    public override int GetHashCode() => 17 * Name.GetHashCode()
                                       + 23 * Label.GetHashCode()
                                       + 31 * Documentation.GetHashCode()
                                       + Parameters.Aggregate(
                                             37,
                                             (
                                                 current,
                                                 element) => current + element.GetHashCode()
                                         );
}

public class QuickInfoRequest : Request { }

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
    private string? _fileName;

    public string FileName
    {
        get => _fileName?.Replace(
            Path.AltDirectorySeparatorChar,
            Path.DirectorySeparatorChar
        ) ?? "";
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

public class QuickInfoResponse
{
    public string Markdown { get; set; } = string.Empty;
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
        return new CompletionItem
        {
            Label            = ci.Label,
            Kind             = (CompletionItemKind)(int)ci.Kind,
            Detail           = ci.Detail,
            Documentation    = ci.Documentation?.ConvertToString(),
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
        return new LinePositionSpanTextChange
        {
            NewText     = text,
            StartColumn = range.Start.Character,
            StartLine   = range.Start.Line,
            EndColumn   = range.End.Character,
            EndLine     = range.End.Line
        };
    }
}

public static class Extensions
{
    public static string ConvertToString(this StringOrMarkupContent stringOrMarkupContent)
    {
        return stringOrMarkupContent.MarkupContent?.Value ?? stringOrMarkupContent.String;
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
    public async Task<SignatureHelpResponse> GetSignatureHelpAsync(
        string code,
        SignatureHelpRequest signatureHelpRequest)
    {
        var visitor = new SignatureHelpVisitor(
            new Position(signatureHelpRequest.Line, signatureHelpRequest.Column),
            StepFactoryStore
        );

        var signatureHelp = visitor.LexParseAndVisit(
            code,
            x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); }
        );

        if (signatureHelp is null)
            return new SignatureHelpResponse();

        return new SignatureHelpResponse
        {
            ActiveParameter = signatureHelp.ActiveParameter ?? 0,
            ActiveSignature = signatureHelp.ActiveSignature ?? 0,
            Signatures      = signatureHelp.Signatures.Select(Convert)
        };

        SignatureHelpItem Convert(SignatureInformation arg)
        {
            return new SignatureHelpItem
            {
                Documentation = arg.Documentation?.ConvertToString(),
                Label         = arg.Label,
                Parameters    = arg.Parameters.Select(ConvertParameterInfo)
            };
        }

        SignatureHelpParameter ConvertParameterInfo(ParameterInformation arg)
        {
            return new SignatureHelpParameter()
            {
                Documentation = arg.Documentation?.ConvertToString(), Label = arg.Label?.Label
            };
        }
    }

    [JSInvokable]
    public async Task<CompletionResolveResponse> GetCompletionResolveAsync(
        CompletionResolveRequest completionResolveRequest)
    {
        return new CompletionResolveResponse { Item = completionResolveRequest.Item };
    }

    [JSInvokable]
    public async Task<QuickInfoResponse> GetQuickInfoAsync(
        string code,
        QuickInfoRequest quickInfoRequest)
    {
        var lazyTypeResolver = HoverVisitor.CreateLazyTypeResolver(code, StepFactoryStore);

        var position = new Position(quickInfoRequest.Line, quickInfoRequest.Column);

        var command = Helpers.GetCommand(code, position);

        if (command is null)
            return new QuickInfoResponse();

        var visitor2 = new HoverVisitor(
            command.Value.newPosition,
            command.Value.positionOffset,
            StepFactoryStore,
            lazyTypeResolver
        );

        var errorListener = new ErrorErrorListener();

        var hover = visitor2.LexParseAndVisit(
            command.Value.command,
            x => { x.RemoveErrorListeners(); },
            x =>
            {
                x.RemoveErrorListeners();
                x.AddErrorListener(errorListener);
            }
        );

        if (hover is not null)
        {
            if (hover.Contents.HasMarkupContent)
                return new QuickInfoResponse { Markdown = hover.Contents.MarkupContent?.Value };

            if (hover.Contents.MarkedStrings is not null)
                return new QuickInfoResponse
                {
                    Markdown = string.Join(
                        "\r\n",
                        hover.Contents.MarkedStrings.Select(x => x.Value)
                    )
                };
        }

        if (errorListener.Errors.Any())
        {
            var error = errorListener.Errors.First();
            return new QuickInfoResponse { Markdown = error.Message };
        }

        return new QuickInfoResponse();
    }
}

public static class DiagnosticsHelper
{
    public static IReadOnlyList<Diagnostic> GetDiagnostics(
        string text,
        StepFactoryStore stepFactoryStore)
    {
        List<Diagnostic> diagnostics;

        Result<IFreezableStep, IError> initialParseResult;

        initialParseResult = SCLParsing2.TryParseStep(text);

        if (initialParseResult.IsSuccess)
        {
            var freezeResult = initialParseResult.Value.TryFreeze(
                SCLRunner.RootCallerMetadata,
                stepFactoryStore
            );

            if (freezeResult.IsSuccess)
            {
                diagnostics = new List<Diagnostic>();
            }

            else
            {
                diagnostics = freezeResult.Error.GetAllErrors()
                    .Select(x => ToDiagnostic(x, new Position(0, 0)))
                    .WhereNotNull()
                    .ToList();
            }
        }
        else
        {
            var commands = Helpers.SplitIntoCommands(text);
            diagnostics = new List<Diagnostic>();

            foreach (var (commandText, commandPosition) in commands)
            {
                var visitor  = new DiagnosticsVisitor();
                var listener = new ErrorErrorListener();

                var parseResult = visitor.LexParseAndVisit(
                    commandText,
                    _ => { },
                    x => { x.AddErrorListener(listener); }
                );

                IList<Diagnostic> newDiagnostics = listener.Errors
                    .Select(x => ToDiagnostic(x, commandPosition))
                    .WhereNotNull()
                    .ToList();

                if (!newDiagnostics.Any())
                    newDiagnostics = parseResult.Select(x => ToDiagnostic(x, commandPosition))
                        .WhereNotNull()
                        .ToList();

                diagnostics.AddRange(newDiagnostics);
            }
        }

        return diagnostics;

        static Diagnostic? ToDiagnostic(SingleError error, Position positionOffset)
        {
            if (error.Location.TextLocation is null)
                return null;

            var range = error.Location.TextLocation.GetRange(
                positionOffset.Line,
                positionOffset.Character
            );

            return new Diagnostic(
                new LinePosition(range.Start.Line, range.Start.Character),
                new LinePosition(range.End.Line,   range.End.Character),
                error.Message,
                8
            );
        }
    }
}
