namespace ApricotFramework.Captcha.Model;

/// <summary>
/// The decision about one challenge, and why.
/// </summary>
public sealed record CaptchaValidationResult
{
    /// <summary>
    /// Gets a value indicating whether every requirement was met.
    /// </summary>
    public required bool Valid { get; init; }

    /// <summary>
    /// Gets the deciding reason, from <see cref="CaptchaRejectionReasons"/>.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the provider's failure codes, where it reported any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets the type of the provider that judged, or null where none was resolved.
    /// </summary>
    /// <remarks>
    /// The type rather than the instance name, so this is safe to report to a client. It is null
    /// exactly when the request was turned away before a provider could be chosen.
    /// </remarks>
    public string? ProviderType { get; init; }

    /// <summary>
    /// Creates a result for a challenge that met every requirement.
    /// </summary>
    /// <param name="providerType">The type of the provider that judged.</param>
    /// <returns>A valid result carrying <see cref="CaptchaRejectionReasons.Ok"/>.</returns>
    public static CaptchaValidationResult Accepted(string? providerType = null)
    {
        return new CaptchaValidationResult
        {
            Valid = true,
            Reason = CaptchaRejectionReasons.Ok,
            ProviderType = providerType
        };
    }

    /// <summary>
    /// Creates a result for a challenge that failed a requirement.
    /// </summary>
    /// <param name="reason">The deciding reason, from <see cref="CaptchaRejectionReasons"/>.</param>
    /// <param name="errors">The provider's failure codes, where it reported any.</param>
    /// <param name="providerType">The type of the provider that judged, where one was resolved.</param>
    /// <returns>An invalid result carrying <paramref name="reason"/>.</returns>
    public static CaptchaValidationResult Rejected(string reason, IReadOnlyList<string>? errors = null, string? providerType = null)
    {
        return new CaptchaValidationResult
        {
            Valid = false,
            Reason = reason,
            Errors = errors ?? [],
            ProviderType = providerType
        };
    }
}
