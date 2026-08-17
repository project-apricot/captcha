using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Providers.HCaptcha;

/// <summary>
/// hCaptcha
/// </summary>
/// <remarks>
/// hCaptcha reports <em>risk</em>, where higher is worse, so the score is inverted onto the shared
/// scale. Only enterprise accounts report one completely.
/// <para>
/// It is also the only provider that checks the site key server side, answering
/// <c>sitekey-secret-mismatch</c> when the configured key does not belong to the secret.
/// </para>
/// </remarks>
public abstract class HCaptchaProviderBase : SiteverifyCaptchaProviderBase
{
    /// <summary>
    /// The address of the verification API
    /// </summary>
    private static readonly Uri Endpoint = new("https://api.hcaptcha.com/siteverify");

    /// <inheritdoc />
    public override string GetProviderType()
    {
        return CaptchaProviderTypes.HCaptcha;
    }

    /// <inheritdoc />
    protected override Uri GetVerificationEndpoint()
    {
        return Endpoint;
    }

    /// <inheritdoc />
    protected override Task ConfigureBodyAsync(IDictionary<string, string> body, CaptchaVerificationInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        // the configured key, never the one the caller claimed; checking a value the caller chose
        // would prove nothing
        var siteKey = this.GetSiteKey();

        if (!string.IsNullOrWhiteSpace(siteKey))
        {
            body["sitekey"] = siteKey;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override double? NormalizeScore(bool success, double? providerScore)
    {
        return 1.0 - providerScore;
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
            "expired-input-response" or "already-seen-response" or "timeout-or-duplicate" => CaptchaProviderErrors.TimeoutOrDuplicate,
            "missing-remoteip" or "invalid-remoteip" => CaptchaProviderErrors.BadRequest,
            "not-using-dummy-passcode" or "sitekey-secret-mismatch" => CaptchaProviderErrors.InvalidKeys,
            _ => CaptchaProviderErrors.UnknownError,
        };
    }
}
