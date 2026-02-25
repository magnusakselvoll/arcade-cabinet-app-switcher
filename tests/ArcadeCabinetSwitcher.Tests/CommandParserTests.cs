using ArcadeCabinetSwitcher.ProcessManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class CommandParserTests
{
    [TestMethod]
    public void Parse_SimpleExe_ReturnsFileNameAndEmptyArguments()
    {
        var (fileName, arguments) = CommandParser.Parse("mame.exe");

        Assert.AreEqual("mame.exe", fileName);
        Assert.AreEqual(string.Empty, arguments);
    }

    [TestMethod]
    public void Parse_ExeWithArguments_ReturnsSplit()
    {
        var (fileName, arguments) = CommandParser.Parse("mame.exe -bigpicture -fullscreen");

        Assert.AreEqual("mame.exe", fileName);
        Assert.AreEqual("-bigpicture -fullscreen", arguments);
    }

    [TestMethod]
    public void Parse_QuotedPath_ReturnsFileNameAndEmptyArguments()
    {
        var (fileName, arguments) = CommandParser.Parse(@"""C:\Program Files\app.exe""");

        Assert.AreEqual(@"C:\Program Files\app.exe", fileName);
        Assert.AreEqual(string.Empty, arguments);
    }

    [TestMethod]
    public void Parse_QuotedPathWithArguments_ReturnsBoth()
    {
        var (fileName, arguments) = CommandParser.Parse(@"""C:\Program Files\app.exe"" -bigpicture");

        Assert.AreEqual(@"C:\Program Files\app.exe", fileName);
        Assert.AreEqual("-bigpicture", arguments);
    }

    [TestMethod]
    public void Parse_EmptyString_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => CommandParser.Parse(""));
    }

    [TestMethod]
    public void Parse_NullString_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => CommandParser.Parse(null!));
    }
}
