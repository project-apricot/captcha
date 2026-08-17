namespace ApricotFramework.Captcha.Model;

/// <summary>
/// The provider types this library implements
/// </summary>
/// <remarks>
/// These are protocol tokens: they travel in the request's type header and are written into
/// configuration, so their spelling is a contract. Instance names are not — those are labels the
/// consuming app invents.
/// </remarks>
public static class CaptchaProviderTypes
{
    /// <summary>
    /// Google reCAPTCHA, v2 or v3
    /// </summary>
    public const string Recaptcha = "recaptcha";

    /// <summary>
    /// hCaptcha
    /// </summary>
    public const string HCaptcha = "hcaptcha";

    /// <summary>
    /// Cloudflare Turnstile
    /// </summary>
    public const string Turnstile = "turnstile";
}
