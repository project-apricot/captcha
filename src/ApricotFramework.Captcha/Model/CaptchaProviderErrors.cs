namespace ApricotFramework.Captcha.Model;

/// <summary>
/// A provider's own failure codes, normalised so the three providers report the same vocabulary.
/// </summary>
/// <remarks>
/// A transport failure is absent by design: it raises <see cref="Exceptions.CaptchaException"/> rather
/// than producing a verification result, so the caller is not told the challenge was rejected when the
/// truth is that nobody was asked.
/// </remarks>
public static class CaptchaProviderErrors
{
    /// <summary>
    /// No secret reached the provider, which nearly always means it is not configured.
    /// </summary>
    public const string MissingSecret = "missing_secret";

    /// <summary>
    /// The secret is malformed or belongs to a different site.
    /// </summary>
    public const string InvalidSecret = "invalid_secret";

    /// <summary>
    /// No challenge token reached the provider.
    /// </summary>
    public const string MissingResponse = "missing_response";

    /// <summary>
    /// The challenge token is malformed.
    /// </summary>
    public const string InvalidResponse = "invalid_response";

    /// <summary>
    /// The provider rejected the shape of the verification request.
    /// </summary>
    public const string BadRequest = "bad_request";

    /// <summary>
    /// The token has expired or has already been redeemed; tokens are single use.
    /// </summary>
    public const string TimeoutOrDuplicate = "timeout_or_duplicate";

    /// <summary>
    /// The secret and the site key do not belong together.
    /// </summary>
    public const string InvalidKeys = "invalid_keys";

    /// <summary>
    /// The provider reported a fault on its own side.
    /// </summary>
    public const string InternalError = "internal_error";

    /// <summary>
    /// The provider returned a code this library does not recognise.
    /// </summary>
    public const string UnknownError = "unknown_error";
}
