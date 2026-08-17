namespace ApricotFramework.Captcha.AspNetCore.Options;

/// <summary>
/// The captcha providers available to the host, bound from the <c>Captcha</c> section
/// </summary>
public class CaptchaOptions
{
    /// <summary>
    /// The configuration section these options bind from
    /// </summary>
    public const string SectionName = "Captcha";

    /// <summary>
    /// How long a provider has to answer before the attempt is abandoned
    /// </summary>
    /// <remarks>
    /// The framework default is 100 seconds, which holds a request open long enough for a slow
    /// provider to become an availability problem of its own.
    /// </remarks>
    public static readonly TimeSpan DefaultVerificationTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the provider instance used when nothing else selects one
    /// </summary>
    /// <remarks>
    /// Names an instance, not a type. With none configured, a request that matches no instance is
    /// rejected rather than guessed at.
    /// </remarks>
    public string? DefaultProvider { get; set; }

    /// <summary>
    /// Gets or sets how long a provider has to answer
    /// </summary>
    public TimeSpan VerificationTimeout { get; set; } = DefaultVerificationTimeout;

    /// <summary>
    /// Gets the provider instances, keyed by the name they are addressed by
    /// </summary>
    /// <remarks>
    /// Get-only, which is the shape the configuration binder populates.
    /// </remarks>
    public IDictionary<string, CaptchaProviderEntry> Providers { get; } =
        new Dictionary<string, CaptchaProviderEntry>(StringComparer.OrdinalIgnoreCase);
}
