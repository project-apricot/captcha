namespace ApricotFramework.Captcha.AspNetCore;

/// <summary>
/// Per-call overrides for a single guard invocation.
/// </summary>
public sealed class CaptchaGuardOptions
{
    /// <summary>
    /// Gets or sets the provider to use, overriding both the request header and the configured default.
    /// </summary>
    /// <remarks>
    /// Pinning this is what stops a caller choosing which provider judges their own challenge.
    /// </remarks>
    public string? Provider { get; set; }
}
