namespace ApricotFramework.Captcha.AspNetCore;

/// <summary>
/// The request headers a client presents a challenge with
/// </summary>
/// <remarks>
/// No <c>X-</c> prefix, which RFC 6648 deprecates for new headers. Where a header appears more than
/// once the last occurrence wins.
/// <para>
/// None of them names a provider instance: those are internal labels, and the server decides which
/// instance verifies. These two describe the challenge so the right one can be found.
/// </para>
/// </remarks>
public static class CaptchaHeaders
{
    /// <summary>
    /// Carries the challenge token obtained from the provider's widget
    /// </summary>
    public const string Response = "Captcha-Response";

    /// <summary>
    /// Names the provider type that issued the token
    /// </summary>
    public const string Type = "Captcha-Type";

    /// <summary>
    /// Names the site key the widget was rendered with
    /// </summary>
    /// <remarks>
    /// A site key is public — it is already in the page source — so naming one grants nothing. It
    /// only selects which configured instance verifies, and a token issued for a different site
    /// fails against that instance's secret.
    /// </remarks>
    public const string SiteKey = "Captcha-SiteKey";
}
