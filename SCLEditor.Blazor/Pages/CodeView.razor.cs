using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Util;
using TypeReference = Reductech.EDR.Core.Internal.TypeReference;

namespace Reductech.Utilities.SCLEditor.Blazor.Pages
{

public partial class CodeView
{
    public string SCLText { get; set; } = "";

    private FileSelection FileSelection;

    private StringBuilder consoleStringBuilder = new();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Console.SetOut(new StringWriter(consoleStringBuilder));
        base.OnInitialized();
    }

    [CanBeNull] public CancellationTokenSource? CancellationTokenSource { get; set; }

    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = null;
    }

    public IExternalContext GetExternalContext()
    {
        var fileSystem = new FileSelectionFileSystem(FileSelection);

        return new ExternalContext(
            fileSystem,
            ExternalContext.Default.ExternalProcessRunner,
            ExternalContext.Default.Console
        );
    }

    public async Task Run()
    {
        //Output = "";
        CancellationTokenSource?.Cancel();
        var cts = new CancellationTokenSource();
        CancellationTokenSource = cts;

        var result = await RunSequenceFromTextAsync(
            SCLText,
            SCLSettings.EmptySettings,
            GetExternalContext(),
            NullLogger.Instance,
            StepFactoryStore.CreateUsingReflection(),
            cts.Token
        );

        CancellationTokenSource = null;

        if (result is Unit)
            consoleStringBuilder.AppendLine("Sequence Completed Successfully");
        else
        {
            consoleStringBuilder.AppendLine(
                $"Sequence Completed Successfully with result: '{result}'"
            );
        }

        consoleStringBuilder.AppendLine();
    }

    public async Task<object> RunSequenceFromTextAsync(
        string text,
        SCLSettings settings,
        IExternalContext externalContext,
        ILogger logger,
        StepFactoryStore stepFactoryStore,
        CancellationToken cancellationToken)
    {
        var stepResult = SCLParsing.ParseSequence(text)
            .Bind(x => x.TryFreeze(TypeReference.Any.Instance, stepFactoryStore));

        if (stepResult.IsFailure)
            return stepResult.Error.AsString;

        await using var stateMonad = new StateMonad(
            logger,
            settings,
            stepFactoryStore,
            externalContext,
            new Dictionary<string, object>()
        );

        var runResult = await stepResult.Value.Run<object>(stateMonad, cancellationToken);

        if (runResult.IsFailure)
            return runResult.Error.AsString;

        return runResult.Value;
    }
}

}
