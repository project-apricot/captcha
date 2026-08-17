namespace ApricotFramework.Captcha.AspNetCore;

/// <summary>
/// The names of the clients each provider type verifies with
/// </summary>
/// <remarks>
/// One per type rather than per instance, since the endpoint is a property of the type. Published so
/// a host can attach its own handlers with the framework's own API:
/// <c>services.AddHttpClient(CaptchaHttpClients.Recaptcha).AddStandardResilienceHandler()</c>.
/// </remarks>
public static class CaptchaHttpClients
{
    /// <summary>
    /// The client Google reCAPTCHA verifies with
    /// </summary>
    public const string Recaptcha = "apricot-captcha-recaptcha";

    /// <summary>
    /// The client hCaptcha verifies with
    /// </summary>
    public const string HCaptcha = "apricot-captcha-hcaptcha";

    /// <summary>
    /// The client Cloudflare Turnstile verifies with
    /// </summary>
    public const string Turnstile = "apricot-captcha-turnstile";
}
