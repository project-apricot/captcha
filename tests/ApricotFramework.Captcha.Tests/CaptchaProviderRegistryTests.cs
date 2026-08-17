using ApricotFramework.Captcha.Impl;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Tests;

public class CaptchaProviderRegistryTests
{
    [Fact]
    public void Find_RegisteredName_ReturnsProvider()
    {
        var registry = new DefaultCaptchaProviderRegistry([new StubProvider("Default")]);

        Assert.NotNull(registry.Find("Default"));
    }

    [Fact]
    public void Find_UnregisteredName_ReturnsNull()
    {
        var registry = new DefaultCaptchaProviderRegistry([new StubProvider("Default")]);

        Assert.Null(registry.Find("Missing"));
    }

    // Instance names are labels the host chose, not protocol tokens, so they match like config keys.
    [Fact]
    public void Find_NameDifferingOnlyByCase_ReturnsProvider()
    {
        var registry = new DefaultCaptchaProviderRegistry([new StubProvider("AdminPortal")]);

        Assert.NotNull(registry.Find("adminportal"));
    }

    [Fact]
    public void Constructor_TwoProvidersWithSameName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DefaultCaptchaProviderRegistry([new StubProvider("Default"), new StubProvider("Default")]));
    }

    [Fact]
    public void Match_NeitherSupplied_ReturnsEveryInstance()
    {
        var registry = Registry();

        Assert.Equal(3, registry.Match(null, null).Count);
    }

    [Fact]
    public void Match_TypeOnly_ReturnsEveryInstanceOfThatType()
    {
        var registry = Registry();

        var matches = registry.Match(CaptchaProviderTypes.Recaptcha, null);

        Assert.Equal(["Default", "Mobile"], matches.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Match_SiteKeyOnly_NarrowsToTheInstanceHoldingIt()
    {
        var registry = Registry();

        Assert.Equal(["Mobile"], registry.Match(null, "key-mobile"));
    }

    [Fact]
    public void Match_TypeAndSiteKey_NarrowsByBoth()
    {
        var registry = Registry();

        Assert.Equal(["Default"], registry.Match(CaptchaProviderTypes.Recaptcha, "key-default"));
    }

    [Fact]
    public void Match_TypeAndSiteKeyDisagreeing_ReturnsNothing()
    {
        var registry = Registry();

        Assert.Empty(registry.Match(CaptchaProviderTypes.Turnstile, "key-default"));
    }

    // Config is hand-written, so being strict about the type's spelling costs more than it buys.
    [Fact]
    public void Match_TypeDifferingOnlyByCase_StillMatches()
    {
        var registry = Registry();

        Assert.Equal(["International"], registry.Match("TurnStile", null));
    }

    // The providers publish one reCAPTCHA test pair, so dev configurations share a site key.
    [Fact]
    public void Match_SiteKeySharedBySeveralInstances_ReturnsAllOfThem()
    {
        var registry = new DefaultCaptchaProviderRegistry(
        [
            new StubProvider("Default", CaptchaProviderTypes.Recaptcha, "shared"),
            new StubProvider("AdminPortal", CaptchaProviderTypes.Recaptcha, "shared"),
        ]);

        Assert.Equal(2, registry.Match(null, "shared").Count);
    }

    [Fact]
    public void Match_UnknownSiteKey_ReturnsNothing()
    {
        Assert.Empty(Registry().Match(null, "never-configured"));
    }

    private static DefaultCaptchaProviderRegistry Registry()
    {
        return new DefaultCaptchaProviderRegistry(
        [
            new StubProvider("Default", CaptchaProviderTypes.Recaptcha, "key-default"),
            new StubProvider("Mobile", CaptchaProviderTypes.Recaptcha, "key-mobile"),
            new StubProvider("International", CaptchaProviderTypes.Turnstile, "key-intl"),
        ]);
    }
}
