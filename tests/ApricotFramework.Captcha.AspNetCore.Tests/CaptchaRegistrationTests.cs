using ApricotFramework.Captcha.AspNetCore.Extensions;
using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Captcha.AspNetCore.Tests;

public class CaptchaRegistrationTests
{
    [Fact]
    public void AddCaptcha_Always_ResolvesTheGuardAndItsCollaborators()
    {
        using var provider = Build();

        Assert.NotNull(provider.GetRequiredService<ICaptchaGuard>());
        Assert.NotNull(provider.GetRequiredService<ICaptchaVerifier>());
        Assert.NotNull(provider.GetRequiredService<ICaptchaProviderRegistry>());
        Assert.NotNull(provider.GetRequiredService<ICaptchaRequirementValidator>());
    }

    [Fact]
    public void AddCaptcha_Always_RegistersTheThreeBuiltInTypes()
    {
        using var provider = Build();

        var types = provider.GetServices<ICaptchaProviderFactory>()
            .Select(factory => factory.GetProviderType())
            .Order(StringComparer.Ordinal);

        Assert.Equal(["hcaptcha", "recaptcha", "turnstile"], types);
    }

    [Fact]
    public void AddCaptcha_CalledTwice_LeavesOneOfEachFactory()
    {
        using var provider = Build(services => services.AddCaptcha(Configuration()));

        Assert.Equal(3, provider.GetServices<ICaptchaProviderFactory>().Count());
    }

    [Fact]
    public void AddCaptcha_Always_BindsEveryConfiguredInstance()
    {
        using var provider = Build();

        var options = provider.GetRequiredService<IOptions<CaptchaOptions>>().Value;

        Assert.Equal(["AdminPortal", "Default", "International"], options.Providers.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(CaptchaProviderTypes.Recaptcha, options.Providers["Default"].Type);
        Assert.Equal("secret-default", options.Providers["Default"].Secret);
        Assert.Equal("key-default", options.Providers["Default"].SiteKey);
        Assert.Equal("Default", options.DefaultProvider);
    }

    // Strict unless a configuration says otherwise. An entry that says nothing about test keys must
    // bind to false, so nothing is relaxed by omission.
    [Fact]
    public void AddCaptcha_EntryNotMentioningTestKeys_BindsToStrict()
    {
        using var provider = Build();

        var options = provider.GetRequiredService<IOptions<CaptchaOptions>>().Value;

        Assert.All(options.Providers.Values, entry => Assert.False(entry.UsesTestKeys));
    }

    [Fact]
    public void AddCaptcha_EntryOptingIn_BindsToRelaxed()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Captcha:Providers:Default:UsesTestKeys"] = "true",
        });

        using var provider = Build(configuration: configuration);

        var options = provider.GetRequiredService<IOptions<CaptchaOptions>>().Value;

        Assert.True(options.Providers["Default"].UsesTestKeys);

        // and only that instance
        Assert.False(options.Providers["AdminPortal"].UsesTestKeys);
    }

    // Instance names are configuration keys, so they match the way configuration keys do.
    [Fact]
    public void Registry_InstanceNameInDifferentCase_StillResolves()
    {
        using var provider = Build();

        var registry = provider.GetRequiredService<ICaptchaProviderRegistry>();

        Assert.NotNull(registry.Find("adminportal"));
    }

    [Fact]
    public void Registry_Find_BuildsAProviderCarryingTheEntrysSettings()
    {
        using var provider = Build();

        var found = provider.GetRequiredService<ICaptchaProviderRegistry>().Find("International");

        Assert.NotNull(found);
        Assert.Equal("International", found.GetProviderName());
        Assert.Equal(CaptchaProviderTypes.Turnstile, found.GetProviderType());
        Assert.Equal("key-intl", found.GetSiteKey());
    }

    [Fact]
    public void Registry_Match_NarrowsBySiteKey()
    {
        using var provider = Build();

        var matches = provider.GetRequiredService<ICaptchaProviderRegistry>().Match(null, "key-admin");

        Assert.Equal(["AdminPortal"], matches);
    }

    // The framework default is 100 seconds, long enough to turn a slow provider into an outage.
    [Fact]
    public void AddCaptcha_Always_BoundsTheClientTimeout()
    {
        using var provider = Build();

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(CaptchaHttpClients.Recaptcha);

        Assert.Equal(CaptchaOptions.DefaultVerificationTimeout, client.Timeout);
    }

    [Fact]
    public void AddCaptcha_WithConfiguredTimeout_UsesIt()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Captcha:VerificationTimeout"] = "00:00:03",
        });

        using var provider = Build(configuration: configuration);

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(CaptchaHttpClients.Turnstile);

        Assert.Equal(TimeSpan.FromSeconds(3), client.Timeout);
    }

    [Fact]
    public void AddCaptchaProviderFactory_CustomType_IsResolvable()
    {
        using var provider = Build(services => services.AddCaptchaProviderFactory<CustomProviderFactory>());

        Assert.Contains(
            provider.GetServices<ICaptchaProviderFactory>(),
            factory => string.Equals(factory.GetProviderType(), "custom", StringComparison.Ordinal));
    }

    [Fact]
    public void AddCaptchaProviderFactory_CustomTypeDeclaredInConfiguration_Resolves()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Captcha:Providers:Homegrown:Type"] = "custom",
            ["Captcha:Providers:Homegrown:Secret"] = "secret-custom",
        });

        using var provider = Build(
            services => services.AddCaptchaProviderFactory<CustomProviderFactory>(),
            configuration);

        var found = provider.GetRequiredService<ICaptchaProviderRegistry>().Find("Homegrown");

        Assert.NotNull(found);
        Assert.Equal("custom", found.GetProviderType());
    }

    private static IConfiguration Configuration(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Captcha:DefaultProvider"] = "Default",
            ["Captcha:Providers:Default:Type"] = CaptchaProviderTypes.Recaptcha,
            ["Captcha:Providers:Default:Secret"] = "secret-default",
            ["Captcha:Providers:Default:SiteKey"] = "key-default",
            ["Captcha:Providers:AdminPortal:Type"] = CaptchaProviderTypes.Recaptcha,
            ["Captcha:Providers:AdminPortal:Secret"] = "secret-admin",
            ["Captcha:Providers:AdminPortal:SiteKey"] = "key-admin",
            ["Captcha:Providers:International:Type"] = CaptchaProviderTypes.Turnstile,
            ["Captcha:Providers:International:Secret"] = "secret-intl",
            ["Captcha:Providers:International:SiteKey"] = "key-intl",
        };

        foreach (var (key, value) in overrides ?? [])
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ServiceProvider Build(Action<IServiceCollection>? configure = null, IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();
        services.AddCaptcha(configuration ?? Configuration());
        configure?.Invoke(services);

        // scope validation, so a singleton capturing a scoped dependency fails here rather than later
        return services.BuildServiceProvider(validateScopes: true);
    }

    private sealed class CustomProviderFactory : ICaptchaProviderFactory
    {
        public string GetProviderType() => "custom";

        public ICaptchaProvider Create(string providerName, CaptchaProviderEntry entry) =>
            new CustomProvider(providerName, entry.SiteKey);
    }

    private sealed class CustomProvider(string name, string? siteKey) : ICaptchaProvider
    {
        public string GetProviderName() => name;

        public string GetProviderType() => "custom";

        public string? GetSiteKey() => siteKey;

        public Task<CaptchaVerificationResult> VerifyAsync(CaptchaVerificationInput input, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CaptchaVerificationResult { Success = true, ProviderName = name, ProviderType = "custom" });
    }
}
