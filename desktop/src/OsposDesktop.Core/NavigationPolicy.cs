namespace OsposDesktop.Core;

public enum NavigationDecision
{
    /// <summary>Load inside the app window.</summary>
    Allow,

    /// <summary>Hand to the system's default browser (help pages, opensourcepos.org, maps ...).</summary>
    OpenExternally,

    /// <summary>Drop silently (javascript:, file:, unknown schemes).</summary>
    Block,
}

/// <summary>
/// Keeps the app window on the configured OSPOS server so it behaves like a dedicated
/// till rather than a general browser.
/// </summary>
public sealed class NavigationPolicy(Uri serverAddress)
{
    public Uri ServerAddress { get; } = serverAddress;

    public NavigationDecision Decide(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return NavigationDecision.Block;
        }

        if (target.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationDecision.Allow;
        }

        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return NavigationDecision.Block;
        }

        if (uri.Scheme == Uri.UriSchemeMailto || uri.Scheme == "tel")
        {
            return NavigationDecision.OpenExternally;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return NavigationDecision.Block;
        }

        return IsSameOrigin(uri) ? NavigationDecision.Allow : NavigationDecision.OpenExternally;
    }

    private bool IsSameOrigin(Uri uri) =>
        string.Equals(uri.Scheme, ServerAddress.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.IdnHost, ServerAddress.IdnHost, StringComparison.OrdinalIgnoreCase)
        && uri.Port == ServerAddress.Port;
}
