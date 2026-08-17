using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.AspNetCore.Providers.Recaptcha;

/// <summary>
/// Builds reCAPTCHA instances
/// </summary>
public class RecaptchaProviderFactory : ICaptchaProviderFactory
{
    /// <summary>
    /// The factory supplying the client instances verify with
    /// </summary>
    protected IHttpClientFactory HttpClientFactory { get; }

    /// <summary>
    /// Creates a new instance of the reCAPTCHA factory
    /// </summary>
    /// <param name="httpClientFactory">Supplies the client instances verify with</param>
    public RecaptchaProviderFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        this.HttpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public virtual string GetProviderType()
    {
        return CaptchaProviderTypes.Recaptcha;
    }

    /// <inheritdoc />
    public virtual ICaptchaProvider Create(string providerName, CaptchaProviderEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RecaptchaProvider(providerName, entry.Secret, entry.SiteKey, entry.UsesTestKeys, this.HttpClientFactory);
    }
}
