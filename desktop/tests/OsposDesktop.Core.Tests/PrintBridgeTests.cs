using OsposDesktop.Core;

namespace OsposDesktop.Core.Tests;

public class PrintBridgeTests
{
    [Fact]
    public void IsPrintRequest_AcceptsPrintMessage()
    {
        Assert.True(PrintBridge.IsPrintRequest("""{"type":"ospos-desktop:print"}"""));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("\"ospos-desktop:print\"")]
    [InlineData("""{"type":"something-else"}""")]
    [InlineData("""{"type":42}""")]
    [InlineData("[]")]
    public void IsPrintRequest_RejectsOtherMessages(string? message)
    {
        Assert.False(PrintBridge.IsPrintRequest(message));
    }

    [Fact]
    public void Script_ReplacesWindowPrintWithHostMessage()
    {
        Assert.Contains("window.print = ", PrintBridge.Script);
        Assert.Contains(PrintBridge.PrintMessageType, PrintBridge.Script);
    }

    [Fact]
    public void MillimetresToInches_Converts()
    {
        Assert.Equal(1.0, PrintBridge.MillimetresToInches(25.4), precision: 10);
    }
}
