namespace ApricotFramework.Captcha.Model;

/// <summary>
/// A provider's ruling, normalised across providers.
/// </summary>
public sealed record CaptchaVerificationResult
{
    /// <summary>
    /// Gets a value indicating whether the provider accepted the token.
    /// </summary>
    /// <remarks>
    /// Acceptance alone is not enough: the score, action, and hostname still have to satisfy the
    /// endpoint's requirements.
    /// </remarks>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the name of the provider instance that produced this result.
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets the type of the provider that produced this result.
    /// </summary>
    public required string ProviderType { get; init; }

    /// <summary>
    /// Gets when the challenge was solved, or null where the provider does not say.
    /// </summary>
    public DateTimeOffset? ChallengeTimestamp { get; init; }

    /// <summary>
    /// Gets the hostname that served the challenge, or null where the provider does not say.
    /// </summary>
    public string? Hostname { get; init; }

    /// <summary>
    /// Gets the confidence score, where 1.0 is the most human, or null where the provider has no score.
    /// </summary>
    /// <remarks>
    /// Normalised, so a provider that reports risk rather than confidence is inverted to this scale.
    /// Only reCAPTCHA v3 and enterprise hCaptcha populate it.
    /// </remarks>
    public double? Score { get; init; }

    /// <summary>
    /// Gets the action the client declared when solving, or null where the provider has no such concept.
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Gets the provider's failure codes, mapped onto <see cref="CaptchaProviderErrors"/>.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether this came from an instance configured with the provider's
    /// published test keys.
    /// </summary>
    /// <remarks>
    /// Test keys verify anything and report no score, no action and a hostname of the provider's own,
    /// so a requirement they cannot answer is treated as met. Nothing the provider did answer is
    /// relaxed. <strong>Never true in production.</strong>
    /// </remarks>
    public bool UsesTestKeys { get; init; }
}
