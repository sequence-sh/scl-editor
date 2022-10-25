using CSharpFunctionalExtensions;
using Reductech.Sequence.Connectors.FileSystem;
using Reductech.Sequence.Core;
using Reductech.Sequence.Core.Abstractions;
using Reductech.Sequence.Core.ExternalProcesses;
using Reductech.Sequence.Core.Internal;
using Reductech.Sequence.Core.Internal.Parser;
using Reductech.Sequence.Core.Internal.Serialization;
using Reductech.Sequence.Core.Util;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// SCL language helper that can also run SCL
/// </summary>
public class RunnableSCLLanguageHelper : SCLLanguageHelper
{
    /// <inheritdoc />
    public RunnableSCLLanguageHelper(
        IJSRuntime runtime,
        IHttpClientFactory httpClientFactory,
        ITestLoggerFactory loggerFactory,
        Func<Task<StepFactoryStore>> createStepFactoryStore,
        StringBuilder consoleStream) : base(
        runtime,
        createStepFactoryStore
    )
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory     = loggerFactory;
        ConsoleStream      = consoleStream;
    }

    /// <summary>
    /// Stream of messages to the console
    /// </summary>
    public StringBuilder ConsoleStream { get; }

    /// <summary>
    /// Called when a run starts
    /// </summary>
    public event EventHandler? OnRunStarted;

    /// <summary>
    /// Called when a run completes
    /// </summary>
    public event EventHandler? OnRunComplete;

    /// <summary>
    /// Called when a run is cancelled
    /// </summary>
    public event EventHandler? OnRunCancelled;

    /// <summary>
    /// Called when new console messages are sent
    /// </summary>
    public event EventHandler? OnNewConsoleMessage;

    /// <summary>
    /// Called when the state changes
    /// </summary>
    public event EventHandler? OnStateChanged;

    /// <summary>
    /// Called when there is a new log message
    /// </summary>
    public event EventHandler? OnLogMessage;

    /// <summary>
    /// Cancellation token for the currently running sequence
    /// </summary>
    public CancellationTokenSource? RunCancellation { get; private set; }

    private readonly ICompression _compression = new CompressionAdapter();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITestLoggerFactory _loggerFactory;

    /// <inheritdoc />
    public override async Task<bool> InitialSetup(IEditorWrapper editorWrapper)
    {
        var iResult = await base.InitialSetup(editorWrapper);

        if (iResult)
        {
            Console.SetOut(new StringWriter(ConsoleStream));

            await Editor.AddActionAsync(
                "runSCL",
                "Run SCL",
                new[] { (int)KeyMode.CtrlCmd | (int)KeyCode.KEY_R },
                null,
                null,
                "SCL",
                1.5,
                // ReSharper disable once AsyncVoidLambda
                async (_, _) => await Run()
            );
        }

        return true;
    }

    /// <summary>
    /// Run the SCL
    /// </summary>
    public async Task Run()
    {
        OnRunStarted?.Invoke(this, EventArgs.Empty);

        var sclText = await Editor.GetCodeAsync();

        RunCancellation?.Cancel();
        var cts = new CancellationTokenSource();
        RunCancellation = cts;
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        var loggerSink = _loggerFactory.CreateLogger("SCL");
        var logger     = new ObservableLogger(loggerSink);

        OnLogMessage?.Invoke(this, EventArgs.Empty);

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(
                x => x.TryFreeze(
                    SCLRunner.RootCallerMetadata,
                    StepFactoryStore,
                    new OptimizationSettings(true, true, InjectedVariables)
                )
            );

        ConsoleStream.AppendLine("Sequence Running\n");
        OnNewConsoleMessage?.Invoke(this, EventArgs.Empty);

        if (stepResult.IsFailure)
        {
            ConsoleStream.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            List<(string, object)> injected = new();

            if (Editor.FileSystem is not null)
            {
                injected.Add((ConnectorInjection.FileSystemKey, Editor.FileSystem));
            }

            injected.Add((ConnectorInjection.CompressionKey, _compression));

            var externalContext = new ExternalContext(
                ExternalProcessRunner.Instance,
                new BlazorRestClientFactory(_httpClientFactory),
                ConsoleAdapter.Instance,
                injected.ToArray()
            );

            await using var stateMonad = new StateMonad(
                logger,
                StepFactoryStore,
                externalContext,
                new Dictionary<string, object>()
            );

            var runResult = await stepResult.Value.Run<ISCLObject>(
                stateMonad,
                RunCancellation.Token
            );

            if (runResult.IsFailure)
                ConsoleStream.AppendLine(runResult.Error.AsString);
            else if (runResult.Value is Unit)
                ConsoleStream.AppendLine("\nSequence Completed Successfully");
            else
                ConsoleStream.AppendLine(runResult.Value.ToString());
        }

        RunCancellation = null;
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        ConsoleStream.AppendLine();
        OnNewConsoleMessage?.Invoke(this, EventArgs.Empty);

        OnRunComplete?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cancels SCL if it is running
    /// </summary>
    public void CancelRun()
    {
        RunCancellation?.Cancel();
        RunCancellation = null;
        OnRunCancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();
        CancelRun();
    }
}
