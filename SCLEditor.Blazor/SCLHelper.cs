using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;
using Reductech.Utilities.SCLEditor.LanguageServer;
using Reductech.Utilities.SCLEditor.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.Blazor;

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
        var position = new LinePosition(completionRequest.Line, completionRequest.Column);
        var visitor  = new CompletionVisitor(position, StepFactoryStore);

        var completionList = visitor.LexParseAndVisit(
            code,
            x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); }
        ) ?? new CompletionResponse();

        return completionList;
    }

    [JSInvokable]
    public async Task<SignatureHelpResponse> GetSignatureHelpAsync(
        string code,
        SignatureHelpRequest signatureHelpRequest)
    {
        var visitor = new SignatureHelpVisitor(
            new LinePosition(signatureHelpRequest.Line, signatureHelpRequest.Column),
            StepFactoryStore
        );

        var signatureHelp = visitor.LexParseAndVisit(
            code,
            x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); }
        ) ?? new SignatureHelpResponse();

        return signatureHelp;
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

        var position = new LinePosition(quickInfoRequest.Line, quickInfoRequest.Column);

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

        var response = visitor2.LexParseAndVisit(
            command.Value.command,
            x => { x.RemoveErrorListeners(); },
            x =>
            {
                x.RemoveErrorListeners();
                x.AddErrorListener(errorListener);
            }
        ) ?? new QuickInfoResponse();

        if (errorListener.Errors.Any())
        {
            var error = errorListener.Errors.First();
            return new QuickInfoResponse { Markdown = error.Message };
        }

        return response;
    }
}

public static class DiagnosticsHelper
{
    public static IReadOnlyList<Diagnostic> GetDiagnostics(
        string text,
        StepFactoryStore stepFactoryStore)
    {
        List<Diagnostic> diagnostics;

        var initialParseResult = SCLParsing2.TryParseStep(text);

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
                    .Select(x => ToDiagnostic(x, new LinePosition(0, 0)))
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

        static Diagnostic? ToDiagnostic(SingleError error, LinePosition positionOffset)
        {
            if (error.Location.TextLocation is null)
                return null;

            var range = error.Location.TextLocation.GetRange(
                positionOffset.Line,
                positionOffset.Character
            );

            return new Diagnostic(
                new LinePosition(range.StartLineNumber, range.StartColumn),
                new LinePosition(range.EndLineNumber,   range.EndColumn),
                error.Message,
                8
            );
        }
    }
}
