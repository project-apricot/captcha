using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.AspNetCore.Providers.HCaptcha;

/// <summary>
/// Builds hCaptcha instances
/// </summary>
public class HCaptchaProviderFactory : ICaptchaProviderFactory
{
    /// <summary>
    /// The factory supplying the client instances verify with
    /// </summary>
    protected IHttpClientFactory HttpClientFactory { get; }

    /// <summary>
    /// Creates a new instance of the hCaptcha factory
    /// </summary>
    /// <param name="httpClientFactory">Supplies the client instances verify with</param>
    public HCaptchaProviderFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        this.HttpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public virtual string GetProviderType()
    {
        return CaptchaProviderTypes.HCaptcha;
    }

    /// <inheritdoc />
    public virtual ICaptchaProvider Create(string providerName, CaptchaProviderEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new HCaptchaProvider(providerName, entry.Secret, entry.SiteKey, entry.UsesTestKeys, this.HttpClientFactory);
    }
}
