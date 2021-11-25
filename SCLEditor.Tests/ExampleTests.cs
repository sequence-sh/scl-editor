using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Reductech.EDR.Connectors.FileSystem;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using Reductech.Utilities.SCLEditor;
using Xunit;

namespace SCLEditor.Tests;

/// <summary>
/// Tests SCL examples
/// </summary>
[AutoTheory.UseTestOutputHelper]
public partial class ExampleTests
{
    /// <summary>
    /// Checks that there are at least two examples
    /// </summary>
    [Fact]
    public void ShouldBeAtLeast2Examples()
    {
        var examples = ExampleList.AllExamples.ToList();

        examples.Count.Should().BeGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Tests that all examples work
    /// </summary>
    [Fact]
    public async Task AllExamplesShouldWork()
    {
        var examples = ExampleList.AllExamples.ToList();

        var sfs = StepFactoryStore.Create();

        foreach (var exampleTemplate in examples)
        {
            var ecd = ExampleChoiceData.Create(exampleTemplate.ExampleComponent);
            await RunExample(sfs, exampleTemplate, ecd, "default");

            foreach (var mode in exampleTemplate.ExampleComponent.GetAllInputs()
                         .OfType<ExampleInput.Mode>())
            {
                foreach (var option in mode.Options.Skip(1))
                {
                    var ecd1 = ExampleChoiceData.Create(exampleTemplate.ExampleComponent);
                    ecd1.ChoiceValues[mode.Name] = option;
                    await RunExample(sfs, exampleTemplate, ecd, $"{mode.Name} {option.Name}");
                }
            }
        }
    }

    private async Task RunExample(
        StepFactoryStore sfs,
        ExampleTemplate exampleTemplate,
        ExampleChoiceData ecd,
        string optionData)
    {
        var mockFileSystem = new MockFileSystem();

        var context = ExternalContext.Default
            with
            {
                InjectedContexts = new (string name, object context)[]
                {
                    (ConnectorInjection.FileSystemKey, mockFileSystem)
                }
            };

        var state = new StateMonad(
            NullLogger.Instance,
            sfs,
            context,
            new Dictionary<string, object>()
        );

        var sequence = exampleTemplate.GetSequence(ecd);

        foreach (var exampleInput in exampleTemplate.ExampleComponent.GetInputs(ecd)
                     .OfType<ExampleInput.ExampleFileInput>())
        {
            mockFileSystem.AddFile(
                exampleInput.Name,
                new MockFileData(exampleInput.InitialValue)
            );
        }

        var result = await sequence.Run<StringStream>(state, CancellationToken.None)
            .Map(x => x.GetStringAsync());

        result.ShouldBeSuccessful();

        TestOutputHelper.WriteLine($"{exampleTemplate.Name} {optionData} -- {result.Value}");
    }

    /// <summary>
    /// Tests documentation xml doc summaries appear
    /// </summary>
    [Fact(Skip = "skip")]
    public void TestDocumentation()
    {
        var logSummary = new Log<int>().StepFactory.Summary;

        logSummary.Should().NotBeNullOrWhiteSpace();

        var printSummary = new Print<int>().StepFactory.Summary;

        printSummary.Should().NotBeNullOrWhiteSpace();
    }
}
