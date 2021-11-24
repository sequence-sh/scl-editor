using System.Linq;
using FluentAssertions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.Utilities.SCLEditor;
using Xunit;

namespace SCLEditor.Tests;

public class ExampleTests
{
    [Fact]
    public void TestSCLExamples()
    {
        var examples = ExampleHelpers.AllExamples.ToList();

        examples.Count.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void TestDocumentation()
    {
        var logSummary = new Log<int>().StepFactory.Summary;

        logSummary.Should().NotBeNullOrWhiteSpace();

        var printSummary = new Print<int>().StepFactory.Summary;

        printSummary.Should().NotBeNullOrWhiteSpace();
    }
}
