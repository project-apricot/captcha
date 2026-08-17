using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Providers.Turnstile;

/// <summary>
/// Cloudflare Turnstile
/// </summary>
/// <remarks>
/// Turnstile is pass or fail and publishes no score, so it always reports none rather than a
/// stand-in. An endpoint that demands a score will reject a Turnstile token; that is the honest
/// answer, and the signal to drop the score requirement or change provider.
/// </remarks>
public abstract class TurnstileProviderBase : SiteverifyCaptchaProviderBase
{
    /// <summary>
    /// The address of the verification API
    /// </summary>
    private static readonly Uri Endpoint = new("https://challenges.cloudflare.com/turnstile/v0/siteverify");

    /// <inheritdoc />
    public override string GetProviderType()
    {
        return CaptchaProviderTypes.Turnstile;
    }

    /// <inheritdoc />
    protected override Uri GetVerificationEndpoint()
    {
        return Endpoint;
    }

    /// <inheritdoc />
    protected override double? NormalizeScore(bool success, double? providerScore)
    {
        return null;
    }

    /// <inheritdoc />
    protected override string MapProviderError(string providerCode)
    {
        return providerCode switch
        {
            "missing-input-secret" => CaptchaProviderErrors.MissingSecret,
            "invalid-input-secret" => CaptchaProviderErrors.InvalidSecret,
            "missing-input-response" => CaptchaProviderErrors.MissingResponse,
            "invalid-input-response" => CaptchaProviderErrors.InvalidResponse,
            "bad-request" => CaptchaProviderErrors.BadRequest,
            "timeout-or-duplicate" => CaptchaProviderErrors.TimeoutOrDuplicate,
            "invalid-parsed-secret" => CaptchaProviderErrors.InvalidSecret,
            "internal-error" => CaptchaProviderErrors.InternalError,
            _ => CaptchaProviderErrors.UnknownError,
        };
    }
}