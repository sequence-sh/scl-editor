using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace SCLEditor.Tests;

public class FileSystemTests
{
    [Fact]
    public void TestMockFileSystem()
    {
        var fs = new MockFileSystem();
    }
}
