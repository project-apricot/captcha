using ApricotFramework.Captcha.Model;
using ApricotFramework.Captcha.Providers.HCaptcha;
using ApricotFramework.Captcha.Providers.Recaptcha;
using ApricotFramework.Captcha.Providers.Turnstile;

namespace ApricotFramework.Captcha.Tests;

internal sealed class TestRecaptchaProvider(HttpClient client, string secret = "secret", string? name = null, string? siteKey = null)
    : RecaptchaProviderBase
{
    public override string GetProviderName() => name ?? base.GetProviderName();

    public override string? GetSiteKey() => siteKey;

    protected override HttpClient GetHttpClient() => client;

    protected override Task<string> GetSecretAsync(CancellationToken cancellationToken) => Task.FromResult(secret);
}

internal sealed class TestHCaptchaProvider(HttpClient client, string? siteKey = null, string? name = null)
    : HCaptchaProviderBase
{
    public override string GetProviderName() => name ?? base.GetProviderName();

    public override string? GetSiteKey() => siteKey;

    protected override HttpClient GetHttpClient() => client;

    protected override Task<string> GetSecretAsync(CancellationToken cancellationToken) => Task.FromResult("secret");
}

internal sealed class TestTurnstileProvider(HttpClient client, string? name = null, string? siteKey = null)
    : TurnstileProviderBase
{
    public override string GetProviderName() => name ?? base.GetProviderName();

    public override string? GetSiteKey() => siteKey;

    protected override HttpClient GetHttpClient() => client;

    protected override Task<string> GetSecretAsync(CancellationToken cancellationToken) => Task.FromResult("secret");
}

/// <summary>
/// A provider that answers without going anywhere, for registry and verifier tests.
/// </summary>
internal sealed class StubProvider(string name, string? providerType = null, string? siteKey = null) : ICaptchaProvider
{
    public string GetProviderName() => name;

    public string GetProviderType() => providerType ?? CaptchaProviderTypes.Recaptcha;

    public string? GetSiteKey() => siteKey;

    public Task<CaptchaVerificationResult> VerifyAsync(CaptchaVerificationInput input, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CaptchaVerificationResult
        {
            Success = true,
            ProviderName = name,
            ProviderType = this.GetProviderType(),
        });
}
