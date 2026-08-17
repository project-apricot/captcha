namespace ApricotFramework.Captcha.ErrorDefinitions;

/// <summary>
/// Every error code this library sends to a client.
/// </summary>
/// <remarks>
/// A client needs message text for exactly these two plus the codes its own service sends. The
/// specific reason travels in the error payload rather than as a code of its own, so the set a client
/// must handle does not grow every time a new way to fail is recognized.
/// </remarks>
public static class CaptchaErrors
{
    /// <summary>
    /// The challenge was judged and did not pass. Reported as a validation failure.
    /// </summary>
    public const string Rejected = "CAPTCHA_REJECTED";

    /// <summary>
    /// The challenge could not be judged at all. Reported as the service being unavailable.
    /// </summary>
    public const string VerificationFailed = "CAPTCHA_VERIFICATION_FAILED";
}
