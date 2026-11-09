using OsposDesktop.Core;

namespace OsposDesktop.Core.Tests;

public class ServerAddressTests
{
    [Theory]
    [InlineData("192.168.1.10", "http://192.168.1.10/")]
    [InlineData("  pos.local:8080  ", "http://pos.local:8080/")]
    [InlineData("pos.local/ospos", "http://pos.local/ospos/")]
    [InlineData("https://shop.example.com/pos/", "https://shop.example.com/pos/")]
    [InlineData("http://localhost/?debug=true#top", "http://localhost/")]
    public void TryNormalize_AcceptsAndNormalizesAddresses(string input, string expected)
    {
        Assert.True(ServerAddress.TryNormalize(input, out var address, out var error));
        Assert.Null(error);
        Assert.Equal(expected, address!.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://pos.local")]
    [InlineData("file:///C:/ospos")]
    [InlineData("http://")]
    public void TryNormalize_RejectsInvalidAddresses(string? input)
    {
        Assert.False(ServerAddress.TryNormalize(input, out var address, out var error));
        Assert.Null(address);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
