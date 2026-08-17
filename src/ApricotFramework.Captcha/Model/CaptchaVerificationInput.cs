namespace ApricotFramework.Captcha.Model;

/// <summary>
/// What a provider needs to rule on a challenge.
/// </summary>
public sealed record CaptchaVerificationInput
{
    /// <summary>
    /// Gets the challenge token the client got from the provider's widget.
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// Gets the address the challenge was solved from, or null to let the provider decide alone.
    /// </summary>
    /// <remarks>
    /// Behind a proxy this must be the forwarded client address; the connection address is the proxy's,
    /// and providers compare it against the solver's, so an unconfigured deployment fails verification.
    /// </remarks>
    public string? RemoteIp { get; init; }

    /// <summary>
    /// Gets the site key the client claims produced the token.
    /// </summary>
    /// <remarks>
    /// <strong>Untrusted and never checked against anything.</strong> It exists so logs can tell which
    /// front end a request came from. The verified equivalent is a site key held in configuration.
    /// </remarks>
    public string? ClaimedSiteKey { get; init; }
}
