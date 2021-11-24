using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlazorMonaco;
using CSharpFunctionalExtensions;
using MELT;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Reductech.EDR.Connectors.FileSystem;
using Reductech.EDR.Connectors.StructuredData;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.ExternalProcesses;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;
using Reductech.Utilities.SCLEditor.LanguageServer;

namespace Reductech.Utilities.SCLEditor.Blazor.Pages;

/// <summary>
/// Playground for using SCL
/// </summary>
public partial class Playground
{
    /// <summary>
    /// The JS runtime
    /// </summary>
    [Inject]
    public IJSRuntime Runtime { get; set; } = null!;

    /// <summary>
    /// The _scl editor to use
    /// </summary>
    private MonacoEditor _sclEditor = null!;

    //private MonacoEditor _fileEditor;

    bool OutputExpanded { get; set; } = true;
    bool LogExpanded { get; set; } = true;

    private readonly ITestLoggerFactory _testLoggerFactory =
        TestLoggerFactory.Create(x => x.FilterByMinimumLevel(LogLevel.Information));

    private readonly MockFileSystem _fileSystem = new();

    private readonly ICompression _compression = new CompressionAdapter();

    private readonly StringBuilder _consoleStringBuilder = new();

    private StepFactoryStore _stepFactoryStore = null!;
    private IExternalContext _externalContext = null!;

    private CancellationTokenSource? RunCancellation { get; set; }

    private SCLCodeHelper _sclCodeHelper = null!;

    private EditorConfiguration _configuration = new();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Console.SetOut(new StringWriter(_consoleStringBuilder));

        _externalContext = new ExternalContext(
            ExternalProcessRunner.Instance,
            DefaultRestClientFactory.Instance,
            ConsoleAdapter.Instance,
            (ConnectorInjection.FileSystemKey, _fileSystem),
            (ConnectorInjection.CompressionKey, _compression)
        );

        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            _externalContext,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        _stepFactoryStore = stepFactoryStoreResult.Value;

        _sclCodeHelper = new SCLCodeHelper(_stepFactoryStore, _configuration);

        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var objRef = DotNetObjectReference.Create(_sclCodeHelper);

            await Runtime.InvokeVoidAsync(
                "registerSCL",
                objRef
            ); //Function Defined in DefineSCLLanguage.js

            var model = await _sclEditor.GetModel();
            await MonacoEditorBase.SetModelLanguage(model, "scl");

            await _sclEditor.AddAction(
                "runSCL",
                "Run SCL",
                new[] { (int)KeyMode.CtrlCmd | (int)KeyCode.KEY_R },
                null,
                null,
                "SCL",
                1.5,
                async (_, _) =>
                {
                    await Run();
                    StateHasChanged();
                }
            );

            await _sclEditor.AddAction(
                "formatscl",
                "Format SCL",
                new[] { (int)KeyMode.CtrlCmd | (int)KeyCode.KEY_F },
                null,
                null,
                "SCL",
                1.5,
                async (_, _) =>
                {
                    await FormatSCL();
                }
            );
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void ClearLogs()
    {
        _testLoggerFactory.Sink.Clear();
    }

    private string LogText()
    {
        var text =
            string.Join(
                "\r\n",
                _testLoggerFactory.Sink.LogEntries.Select(x => x.Message)
            );

        return text;
    }

    private Task OnDidChangeModelContentAsync() =>
        _sclCodeHelper.SetDiagnostics(_sclEditor, Runtime);

    private void CancelRun()
    {
        RunCancellation?.Cancel();
        RunCancellation = null;
    }

    private async Task SetSCL(string s)
    {
        await _sclEditor.SetValue(s);
    }

    private async Task FormatSCL()
    {
        var sclText = await _sclEditor.GetValue();

        var selections = await _sclEditor.GetSelections();

        var uri = (await _sclEditor.GetModel()).Uri;

        var edits = Formatter
            .FormatDocument(sclText, _stepFactoryStore)
            .ToList();

        await _sclEditor.ExecuteEdits(uri, edits, selections);
    }

    private async Task Run()
    {
        var sclText = await _sclEditor.GetValue();

        RunCancellation?.Cancel();
        var cts = new CancellationTokenSource();
        RunCancellation = cts;

        var logger = _testLoggerFactory.CreateLogger("SCL");

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, _stepFactoryStore));

        if (stepResult.IsFailure)
        {
            _consoleStringBuilder.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            await using var stateMonad = new StateMonad(
                logger,
                _stepFactoryStore,
                _externalContext,
                new Dictionary<string, object>()
            );

            var runResult = await stepResult.Value.Run<object>(
                stateMonad,
                RunCancellation.Token
            );

            if (runResult.IsFailure)
                _consoleStringBuilder.AppendLine(runResult.Error.AsString);

            else if (runResult.Value is Unit)
                _consoleStringBuilder.AppendLine("Sequence Completed Successfully");
            else
            {
                _consoleStringBuilder.AppendLine(runResult.Value.ToString());
            }
        }

        RunCancellation = null;

        _consoleStringBuilder.AppendLine();
    }

    private static StandaloneEditorConstructionOptions SCLEditorConstructionOptions(MonacoEditor _)
    {
        return new()
        {
            AutomaticLayout = true,
            Language        = "scl",
            Value = @"- print 123
- log 456"
        };
    }

    //    private static StandaloneEditorConstructionOptions FileEditorConstructionOptions(MonacoEditor _)
    //{
    //    if (_fileSelection?.SelectedFile is not null)
    //    {
    //        var extension = GetLanguageFromFileExtension(
    //            _fileSystem.Path.GetExtension(_fileSelection.SelectedFile.Path)
    //        );

    //        return new()
    //        {
    //            AutomaticLayout = true,
    //            Language        = extension,
    //            Value           = _fileSelection.SelectedFile.Data.TextContents
    //        };
    //    }

    //    return new() { AutomaticLayout = true };
    //}

    private static string GetLanguageFromFileExtension(string extension)
    {
        return extension?.ToLowerInvariant() switch

        {
            "yml"  => "yaml",
            "yaml" => "yaml",
            "json" => "json",
            "cs"   => "csharp",
            _      => ""
        };
    }
}
