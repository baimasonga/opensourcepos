using OsposDesktop.Core;

namespace OsposDesktop.Core.Tests;

public class NavigationPolicyTests
{
    private readonly NavigationPolicy policy = new(new Uri("http://192.168.1.10:8080/ospos/"));

    [Theory]
    [InlineData("http://192.168.1.10:8080/ospos/sales")]
    [InlineData("http://192.168.1.10:8080/other-path")]
    [InlineData("HTTP://192.168.1.10:8080/ospos/items?x=1")]
    [InlineData("about:blank")]
    [InlineData("data:text/html,receipt")]
    [InlineData("blob:http://192.168.1.10:8080/abc")]
    public void Decide_AllowsServerPages(string target)
    {
        Assert.Equal(NavigationDecision.Allow, policy.Decide(target));
    }

    [Theory]
    [InlineData("https://opensourcepos.org")]
    [InlineData("https://192.168.1.10:8080/ospos/")]
    [InlineData("http://192.168.1.10/ospos/")]
    [InlineData("http://192.168.1.11:8080/ospos/")]
    [InlineData("mailto:shop@example.com")]
    public void Decide_OpensOtherSitesExternally(string target)
    {
        Assert.Equal(NavigationDecision.OpenExternally, policy.Decide(target));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/win.ini")]
    [InlineData("ms-settings:printers")]
    [InlineData("not a url")]
    public void Decide_BlocksUnsafeTargets(string? target)
    {
        Assert.Equal(NavigationDecision.Block, policy.Decide(target));
    }
}
