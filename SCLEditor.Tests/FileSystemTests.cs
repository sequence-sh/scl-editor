using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Reductech.Utilities.SCLEditor;
using Xunit;

namespace SCLEditor.Tests
{

public class ResourceTests
{
    [Fact]
    public void TestSCLExamples()
    {
        var examples = ExampleHelpers.AllExamples.ToList();

        examples.Count.Should().BeGreaterOrEqualTo(2);
    }
}

public class FileSystemTests
{
    [Fact]
    public void TestMockFileSystem()
    {
        var fs = new MockFileSystem();
    }
}

}
