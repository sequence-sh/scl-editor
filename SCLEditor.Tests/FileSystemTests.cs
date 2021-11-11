using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SCLEditor.Tests
{

public class FileSystemTests
{
    [Fact]
    public void TestMockFileSystem()
    {
        var fs = new MockFileSystem();
    }
}

}
