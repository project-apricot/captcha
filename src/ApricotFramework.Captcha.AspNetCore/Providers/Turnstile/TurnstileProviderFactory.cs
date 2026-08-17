using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.AspNetCore.Providers.Turnstile;

/// <summary>
/// Builds Turnstile instances
/// </summary>
public class TurnstileProviderFactory : ICaptchaProviderFactory
{
    /// <summary>
    /// The factory supplying the client instances verify with
    /// </summary>
    protected IHttpClientFactory HttpClientFactory { get; }

    /// <summary>
    /// Creates a new instance of the Turnstile factory
    /// </summary>
    /// <param name="httpClientFactory">Supplies the client instances verify with</param>
    public TurnstileProviderFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        this.HttpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public virtual string GetProviderType()
    {
        return CaptchaProviderTypes.Turnstile;
    }

    /// <inheritdoc />
    public virtual ICaptchaProvider Create(string providerName, CaptchaProviderEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new TurnstileProvider(providerName, entry.Secret, entry.SiteKey, entry.UsesTestKeys, this.HttpClientFactory);
    }
}
