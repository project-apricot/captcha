namespace ApricotFramework.Captcha.Model;

/// <summary>
/// Why this library accepted or rejected a challenge, as opposed to what the provider reported.
/// </summary>
/// <remarks>
/// These travel to clients in the error payload, so they are a wire contract. Compare
/// <see cref="CaptchaProviderErrors"/>, which carries the provider's own vocabulary.
/// </remarks>
public static class CaptchaRejectionReasons
{
    /// <summary>
    /// Every requirement was met.
    /// </summary>
    public const string Ok = "ok";

    /// <summary>
    /// No provider was named by the request, the endpoint, or the configured default.
    /// </summary>
    public const string UnknownProvider = "unknown_provider";

    /// <summary>
    /// The request described a provider the endpoint's pinned one does not match.
    /// </summary>
    public const string ProviderMismatch = "provider_mismatch";

    /// <summary>
    /// The request matched several providers, and none of them is the configured default; sending a
    /// site key narrows it.
    /// </summary>
    public const string AmbiguousProvider = "ambiguous_provider";

    /// <summary>
    /// The request carried no challenge token.
    /// </summary>
    public const string MissingResponse = "missing_response";

    /// <summary>
    /// The provider itself did not accept the token.
    /// </summary>
    public const string NotVerified = "not_verified";

    /// <summary>
    /// The score was below the required threshold.
    /// </summary>
    public const string LowScore = "low_score";

    /// <summary>
    /// A score was required, but the provider reports none; usually the wrong provider for the endpoint.
    /// </summary>
    public const string ScoreUnavailable = "score_unavailable";

    /// <summary>
    /// The action was not in the endpoint's allowlist.
    /// </summary>
    public const string ActionMismatch = "action_mismatch";

    /// <summary>
    /// An action was required, but the provider reports none, as with reCAPTCHA v2.
    /// </summary>
    public const string ActionUnavailable = "action_unavailable";

    /// <summary>
    /// The hostname that served the challenge was not in the endpoint's allowlist.
    /// </summary>
    public const string HostnameMismatch = "hostname_mismatch";
}
