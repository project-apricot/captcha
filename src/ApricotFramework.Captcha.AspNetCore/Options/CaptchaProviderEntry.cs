using System.ComponentModel.DataAnnotations;

namespace ApricotFramework.Captcha.AspNetCore.Options;

/// <summary>
/// One configured provider instance
/// </summary>
/// <remarks>
/// The key it is declared under is its name — a label the host invents, never sent by a client.
/// </remarks>
public class CaptchaProviderEntry
{
    /// <summary>
    /// Gets or sets the provider type this instance is of
    /// </summary>
    /// <remarks>
    /// A value from <see cref="Model.CaptchaProviderTypes"/>, or one a registered factory of your own
    /// answers to. Always explicit: the type belongs to the secret, not to the name above it.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret that authenticates this service to the provider
    /// </summary>
    /// <remarks>
    /// Required, and checked at startup. Left blank it would reach the provider as an empty secret,
    /// come back as <c>missing_secret</c>, and be reported to the user as a failed captcha.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the site key whose challenges this instance verifies
    /// </summary>
    /// <remarks>
    /// Optional, but it is what lets a request be matched to this instance rather than to another of
    /// the same type. hCaptcha additionally sends it, so a key not belonging to the secret is caught.
    /// <para>
    /// Two instances may share one; the providers publish only a single reCAPTCHA test pair, so
    /// requiring uniqueness would break every development configuration.
    /// </para>
    /// </remarks>
    public string? SiteKey { get; set; }

    /// <summary>
    /// Gets or sets whether this instance is configured with the provider's published test keys
    /// </summary>
    /// <remarks>
    /// <strong>For troubleshooting only, and false unless you say otherwise.</strong> Test keys verify
    /// any token and report no score, no action, and a hostname belonging to the provider, so an
    /// endpoint declaring a score tier, an action or a hostname could never be exercised with them.
    /// Setting this treats those three as met — and only those three. A token the provider actually
    /// rejected, a score it reported as too low, or an action it reported as the wrong one still fail.
    /// <para>
    /// Belongs in <c>appsettings.Development.json</c>. It is a per-instance setting, so a real
    /// provider alongside it is unaffected, and an environment variable such as
    /// <c>Captcha__Providers__Default__UsesTestKeys=false</c> overrides it anywhere.
    /// </para>
    /// </remarks>
    public bool UsesTestKeys { get; set; }
}
