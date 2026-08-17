using ApricotFramework.Captcha.Providers.Recaptcha;

namespace ApricotFramework.Captcha.AspNetCore.Providers.Recaptcha;

/// <summary>
/// A configured reCAPTCHA instance
/// </summary>
public class RecaptchaProvider : RecaptchaProviderBase
{
    /// <summary>
    /// The name this instance is addressed by
    /// </summary>
    protected string Name { get; }

    /// <summary>
    /// The secret this instance verifies with
    /// </summary>
    protected string Secret { get; }

    /// <summary>
    /// The site key whose challenges this instance verifies
    /// </summary>
    protected string? SiteKey { get; }

    /// <summary>
    /// Whether this instance is configured with the provider's published test keys
    /// </summary>
    protected bool TestKeys { get; }

    /// <summary>
    /// The factory supplying the client to verify with
    /// </summary>
    protected IHttpClientFactory HttpClientFactory { get; }

    /// <summary>
    /// Creates a new instance of the reCAPTCHA provider
    /// </summary>
    /// <param name="name">The name this instance is addressed by</param>
    /// <param name="secret">The secret this instance verifies with</param>
    /// <param name="siteKey">The site key whose challenges this instance verifies</param>
    /// <param name="usesTestKeys">Whether the keys are the provider's published test pair</param>
    /// <param name="httpClientFactory">Supplies the client to verify with</param>
    public RecaptchaProvider(string name, string secret, string? siteKey, bool usesTestKeys, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        this.Name = name;
        this.Secret = secret;
        this.SiteKey = siteKey;
        this.TestKeys = usesTestKeys;
        this.HttpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public override string GetProviderName()
    {
        return this.Name;
    }

    /// <inheritdoc />
    public override string? GetSiteKey()
    {
        return this.SiteKey;
    }

    /// <inheritdoc />
    protected override bool UsesTestKeys()
    {
        return this.TestKeys;
    }

    /// <inheritdoc />
    protected override HttpClient GetHttpClient()
    {
        return this.HttpClientFactory.CreateClient(CaptchaHttpClients.Recaptcha);
    }

    /// <inheritdoc />
    protected override Task<string> GetSecretAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(this.Secret);
    }
}
