using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Providers.Recaptcha;

/// <summary>
/// Google reCAPTCHA, v2 or v3
/// </summary>
/// <remarks>
/// v3 reports a confidence score and an action; v2 reports neither, so an endpoint demanding either
/// will reject a v2 token rather than accept it unmeasured.
/// </remarks>
public abstract class RecaptchaProviderBase : SiteverifyCaptchaProviderBase
{
    /// <summary>
    /// The address of the verification API
    /// </summary>
    private static readonly Uri Endpoint = new("https://www.google.com/recaptcha/api/siteverify");

    /// <inheritdoc />
    public override string GetProviderType()
    {
        return CaptchaProviderTypes.Recaptcha;
    }

    /// <inheritdoc />
    protected override Uri GetVerificationEndpoint()
    {
        return Endpoint;
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
            _ => CaptchaProviderErrors.UnknownError,
        };
    }
}