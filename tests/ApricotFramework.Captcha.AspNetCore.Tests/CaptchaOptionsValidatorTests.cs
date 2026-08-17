using ApricotFramework.Captcha.AspNetCore.Impl;
using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApricotFramework.Captcha.AspNetCore.Tests;

public class CaptchaOptionsValidatorTests
{
    [Fact]
    public void Validate_WellFormedConfiguration_Succeeds()
    {
        var result = Validate(Options());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_NoProvidersAtAll_Succeeds()
    {
        // a host may register captcha and configure it per environment
        Assert.True(Validate(new CaptchaOptions()).Succeeded);
    }

    [Fact]
    public void Validate_UnknownType_Fails()
    {
        var options = Options();
        options.Providers["Rogue"] = new CaptchaProviderEntry { Type = "notaprovider", Secret = "s" };

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("notaprovider", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingType_Fails()
    {
        var options = Options();
        options.Providers["Rogue"] = new CaptchaProviderEntry { Type = string.Empty, Secret = "s" };

        Assert.True(Validate(options).Failed);
    }

    // A blank secret reaches the provider and comes back as a rejected captcha, blaming the visitor.
    [Fact]
    public void Validate_MissingSecret_Fails()
    {
        var options = Options();
        options.Providers["Rogue"] = new CaptchaProviderEntry { Type = CaptchaProviderTypes.Recaptcha, Secret = "  " };

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("Rogue", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DefaultProviderNamingNothing_Fails()
    {
        var options = Options();
        options.DefaultProvider = "Missing";

        Assert.True(Validate(options).Failed);
    }

    [Fact]
    public void Validate_DefaultProviderInDifferentCase_Succeeds()
    {
        var options = Options();
        options.DefaultProvider = "default";

        Assert.True(Validate(options).Succeeded);
    }

    [Fact]
    public void Validate_NoDefaultProvider_Succeeds()
    {
        var options = Options();
        options.DefaultProvider = null;

        Assert.True(Validate(options).Succeeded);
    }

    // reCAPTCHA publishes exactly one test pair, so every development configuration with more than
    // one reCAPTCHA instance shares a site key. Requiring uniqueness would break all of them.
    [Fact]
    public void Validate_TwoInstancesSharingASiteKey_Succeeds()
    {
        var options = Options();
        options.Providers["AdminPortal"] = new CaptchaProviderEntry
        {
            Type = CaptchaProviderTypes.Recaptcha,
            Secret = "secret-admin",
            SiteKey = "shared-key",
        };
        options.Providers["Default"].SiteKey = "shared-key";

        Assert.True(Validate(options).Succeeded);
    }

    // hCaptcha's own test keys share one secret across three site keys.
    [Fact]
    public void Validate_TwoInstancesSharingASecret_Succeeds()
    {
        var options = Options();
        options.Providers["Second"] = new CaptchaProviderEntry
        {
            Type = CaptchaProviderTypes.HCaptcha,
            Secret = options.Providers["Default"].Secret,
        };

        Assert.True(Validate(options).Succeeded);
    }

    // A legitimate development setting, so it warns rather than refusing to start.
    [Fact]
    public void Validate_UsesTestKeys_Succeeds()
    {
        var options = Options();
        options.Providers["Default"].UsesTestKeys = true;

        Assert.True(Validate(options).Succeeded);
    }

    [Fact]
    public void Validate_NoSiteKeyConfigured_Succeeds()
    {
        var options = Options();
        options.Providers["Default"].SiteKey = null;

        Assert.True(Validate(options).Succeeded);
    }

    private static CaptchaOptions Options()
    {
        var options = new CaptchaOptions { DefaultProvider = "Default" };

        options.Providers["Default"] = new CaptchaProviderEntry
        {
            Type = CaptchaProviderTypes.Recaptcha,
            Secret = "secret-default",
            SiteKey = "key-default",
        };

        return options;
    }

    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(CaptchaOptions options)
    {
        var validator = new CaptchaOptionsValidator(
            [
                new NamedFactory(CaptchaProviderTypes.Recaptcha),
                new NamedFactory(CaptchaProviderTypes.HCaptcha),
                new NamedFactory(CaptchaProviderTypes.Turnstile),
            ],
            NullLogger<CaptchaOptionsValidator>.Instance);

        return validator.Validate(name: null, options);
    }

    private sealed class NamedFactory(string type) : ICaptchaProviderFactory
    {
        public string GetProviderType() => type;

        public ICaptchaProvider Create(string providerName, CaptchaProviderEntry entry) =>
            throw new NotSupportedException("Validation never builds a provider.");
    }
}
