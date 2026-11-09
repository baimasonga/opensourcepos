namespace OsposDesktop.Core;

/// <summary>
/// Turns what a user types for the OSPOS server ("192.168.1.10", "pos.local:8080/ospos")
/// into an absolute http(s) URI ending in a slash, so relative OSPOS routes resolve under it.
/// </summary>
public static class ServerAddress
{
    public static bool TryNormalize(string? input, out Uri? address, out string? error)
    {
        address = null;
        error = null;

        var text = input?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            error = "Enter the address of your OSPOS server.";
            return false;
        }

        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "http://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            error = "That is not a valid address.";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            error = "The address must start with http:// or https://.";
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
        };

        if (!builder.Path.EndsWith('/'))
        {
            builder.Path += "/";
        }

        address = builder.Uri;
        return true;
    }
}
