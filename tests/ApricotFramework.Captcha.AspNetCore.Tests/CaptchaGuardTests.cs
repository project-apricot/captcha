using ApricotFramework.Captcha.AspNetCore.Impl;
using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.Captcha.Impl;
using ApricotFramework.Captcha.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Captcha.AspNetCore.Tests;

public class CaptchaGuardTests
{
    [Fact]
    public async Task ValidateAsync_NothingToResolveBy_RejectsAsUnknownProvider()
    {
        var guard = Guard(out var verifier, defaultProvider: null);

        var result = await Validate(guard, Context());

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.UnknownProvider, result.Reason);
        Assert.Equal(0, verifier.Calls);
    }

    [Fact]
    public async Task ValidateAsync_NoHeaders_UsesTheConfiguredDefault()
    {
        var guard = Guard(out var verifier);

        await Validate(guard, Context((CaptchaHeaders.Response, "token")));

        Assert.Equal("Default", verifier.LastProvider);
    }

    [Fact]
    public async Task ValidateAsync_NoToken_RejectsWithoutAskingTheProvider()
    {
        var guard = Guard(out var verifier);

        var result = await Validate(guard, Context((CaptchaHeaders.Type, "recaptcha")));

        Assert.Equal(CaptchaRejectionReasons.MissingResponse, result.Reason);
        Assert.Equal(0, verifier.Calls);
    }

    [Fact]
    public async Task ValidateAsync_SiteKeyIdentifyingOneInstance_UsesIt()
    {
        var guard = Guard(out var verifier);

        await Validate(guard, Context(
            (CaptchaHeaders.Response, "token"),
            (CaptchaHeaders.SiteKey, "key-mobile")));

        Assert.Equal("Mobile", verifier.LastProvider);
    }

    [Fact]
    public async Task ValidateAsync_TypeIdentifyingOneInstance_UsesIt()
    {
        var guard = Guard(out var verifier);

        await Validate(guard, Context(
            (CaptchaHeaders.Response, "token"),
            (CaptchaHeaders.Type, "turnstile")));

        Assert.Equal("International", verifier.LastProvider);
    }

    // Adding a second instance of a type must not break clients that only send a type.
    [Fact]
    public async Task ValidateAsync_TypeMatchingSeveralIncludingTheDefault_UsesTheDefault()
    {
        var guard = Guard(out var verifier);

        await Validate(guard, Context(
            (CaptchaHeaders.Response, "token"),
            (CaptchaHeaders.Type, "recaptcha")));

        Assert.Equal("Default", verifier.LastProvider);
    }

    [Fact]
    public async Task ValidateAsync_MatchingSeveralWithoutTheDefault_RejectsAsAmbiguous()
    {
        var guard = Guard(out var verifier, defaultProvider: "International");

        var result = await Validate(guard, Context(
            (CaptchaHeaders.Response, "token"),
            (CaptchaHeaders.Type, "recaptcha")));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.AmbiguousProvider, result.Reason);
        Assert.Equal(0, verifier.Calls);
    }

    // The providers publish one reCAPTCHA test pair, so a dev config shares a site key.
    [Fact]
    public async Task ValidateAsync_SiteKeySharedBySeveralIncludingTheDefault_UsesTheDefault()
    {
        var guard = Guard(out var verifier, entries: new Dictionary<string, CaptchaProviderEntry>
        {
            ["Default"] = Entry(CaptchaProviderTypes.Recaptcha, "shared"),
            ["AdminPortal"] = Entry(CaptchaProviderTypes.Recaptcha, "shared"),
        });

        var result = await Validate(guard, Context(
            (CaptchaHeaders.Response, "token"),
            (CaptchaHeaders.SiteKey, "shared")));

        Assert.True(result.Valid);
        Assert.Equal("Default", verifier.LastProvider);
    }

    [Fact]
    public async Task ValidateAsync_UnknownSiteKey_RejectsAsUnknownProvider()
    {
        var guard = Guard(out var verifier);

        var result = await Validate(guard, Context(
            (CaptchaHeaders.Response, "token"),
            (CaptchaHeaders.SiteKey, "never-configured")));

        Assert.Equal(CaptchaRejectionReasons.UnknownProvider, result.Reason);
        Assert.Equal(0, verifier.Calls);
    }

    [Fact]
    public async Task ValidateAsync_TypeAndSiteKeyDisagreeing_RejectsAsUnknownProvider()
    {
        var guard = Guard(out _);

        var result = await Validate(guard, Context(
            (CaptchaHeaders.Response, "token"),
            (CaptchaHeaders.Type, "turnstile"),
            (CaptchaHeaders.SiteKey, "key-mobile")));

        Assert.Equal(CaptchaRejectionReasons.UnknownProvider, result.Reason);
    }

    // The default would otherwise have been chosen, and the type alone matches three instances.
    [Fact]
    public async Task ValidateAsync_PinnedProvider_WinsOverWhatTheRequestWouldHaveSelected()
    {
        var guard = Guard(out var verifier);

        await Validate(
            guard,
            Context((CaptchaHeaders.Response, "token"), (CaptchaHeaders.Type, "recaptcha")),
            options: new CaptchaGuardOptions { Provider = "AdminPortal" });

        Assert.Equal("AdminPortal", verifier.LastProvider);
    }

    // Instance names are labels the host chose, so a casing difference is not a bug to hunt.
    [Fact]
    public async Task ValidateAsync_PinnedProviderInDifferentCase_StillResolves()
    {
        var guard = Guard(out var verifier);

        await Validate(
            guard,
            Context((CaptchaHeaders.Response, "token")),
            options: new CaptchaGuardOptions { Provider = "adminportal" });

        Assert.Equal("AdminPortal", verifier.LastProvider);
    }

    // Failing here says why, where the provider would only report that the token was invalid.
    [Fact]
    public async Task ValidateAsync_PinnedProviderWithConflictingType_RejectsAsMismatch()
    {
        var guard = Guard(out var verifier);

        var result = await Validate(
            guard,
            Context((CaptchaHeaders.Response, "token"), (CaptchaHeaders.Type, "turnstile")),
            options: new CaptchaGuardOptions { Provider = "AdminPortal" });

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.ProviderMismatch, result.Reason);
        Assert.Equal(0, verifier.Calls);
    }

    [Fact]
    public async Task ValidateAsync_PinnedProviderWithConflictingSiteKey_RejectsAsMismatch()
    {
        var guard = Guard(out var verifier);

        var result = await Validate(
            guard,
            Context((CaptchaHeaders.Response, "token"), (CaptchaHeaders.SiteKey, "key-mobile")),
            options: new CaptchaGuardOptions { Provider = "AdminPortal" });

        Assert.Equal(CaptchaRejectionReasons.ProviderMismatch, result.Reason);
        Assert.Equal(0, verifier.Calls);
    }

    [Fact]
    public async Task ValidateAsync_PinnedProviderWithAgreeingHeaders_Accepts()
    {
        var guard = Guard(out var verifier);

        var result = await Validate(
            guard,
            Context(
                (CaptchaHeaders.Response, "token"),
                (CaptchaHeaders.Type, "recaptcha"),
                (CaptchaHeaders.SiteKey, "key-admin")),
            options: new CaptchaGuardOptions { Provider = "AdminPortal" });

        Assert.True(result.Valid);
        Assert.Equal("AdminPortal", verifier.LastProvider);
    }

    // A typo in an attribute is our bug, so it must not be reported as the visitor's failed captcha.
    [Fact]
    public async Task ValidateAsync_PinnedProviderThatIsNotConfigured_ThrowsRatherThanRejecting()
    {
        var guard = Guard(out _);

        var exception = await Assert.ThrowsAsync<CaptchaException>(() => Validate(
            guard,
            Context((CaptchaHeaders.Response, "token")),
            options: new CaptchaGuardOptions { Provider = "NeverConfigured" }));

        Assert.IsNotType<CaptchaRejectedException>(exception);
    }

    [Fact]
    public async Task ValidateAsync_RepeatedHeader_TakesTheLastValue()
    {
        var guard = Guard(out var verifier);
        var context = Context();
        context.Request.Headers[CaptchaHeaders.Type] = new[] { "turnstile", "recaptcha" };
        context.Request.Headers[CaptchaHeaders.Response] = new[] { "first", "second" };

        await Validate(guard, context);

        Assert.Equal("Default", verifier.LastProvider);
        Assert.Equal("second", verifier.LastInput!.Response);
    }

    [Fact]
    public async Task ValidateAsync_Always_PassesTheRemoteAddress()
    {
        var guard = Guard(out var verifier);
        var context = Context((CaptchaHeaders.Response, "token"));
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.7");

        await Validate(guard, context);

        Assert.Equal("203.0.113.7", verifier.LastInput!.RemoteIp);
    }

    // Tokens are single use: asking twice has the provider reject the second as a duplicate.
    [Fact]
    public async Task ValidateAsync_CalledTwiceForOneRequest_AsksTheProviderOnce()
    {
        var guard = Guard(out var verifier);
        var context = Context((CaptchaHeaders.Response, "token"));

        await Validate(guard, context);
        await Validate(guard, context);

        Assert.Equal(1, verifier.Calls);
    }

    // Reusing the ruling must not mean the second set of requirements goes unapplied.
    [Fact]
    public async Task ValidateAsync_CalledTwiceWithDifferentRequirements_AppliesBoth()
    {
        var guard = Guard(out var verifier);
        verifier.Result = Verification(action: "sign_in");
        var context = Context((CaptchaHeaders.Response, "token"));

        var permissive = await Validate(guard, context);
        var restrictive = await Validate(guard, context, new CaptchaRequirements { AllowedActions = ["sign_up"] });

        Assert.True(permissive.Valid);
        Assert.False(restrictive.Valid);
        Assert.Equal(CaptchaRejectionReasons.ActionMismatch, restrictive.Reason);
        Assert.Equal(1, verifier.Calls);
    }

    [Fact]
    public async Task ValidateAsync_DifferentTokenOnTheSameContext_AsksAgain()
    {
        var guard = Guard(out var verifier);
        var context = Context((CaptchaHeaders.Response, "first"));

        await Validate(guard, context);
        context.Request.Headers[CaptchaHeaders.Response] = "second";
        await Validate(guard, context);

        Assert.Equal(2, verifier.Calls);
    }

    [Fact]
    public async Task EnsureAsync_Rejected_ThrowsCarryingTheReason()
    {
        var guard = Guard(out var verifier);
        verifier.Result = Verification(success: false);

        var exception = await Assert.ThrowsAsync<CaptchaRejectedException>(() =>
            Ensure(guard, Context((CaptchaHeaders.Response, "token"))));

        Assert.Equal(CaptchaRejectionReasons.NotVerified, exception.Reason);
    }

    // An outage must stay an outage rather than becoming a rejection the user is told to retry.
    [Fact]
    public async Task EnsureAsync_ProviderUnreachable_ThrowsCaptchaExceptionNotARejection()
    {
        var guard = Guard(out var verifier);
        verifier.Throws = new CaptchaException("unreachable", "Default", CaptchaProviderTypes.Recaptcha);

        var exception = await Assert.ThrowsAsync<CaptchaException>(() =>
            Ensure(guard, Context((CaptchaHeaders.Response, "token"))));

        Assert.IsNotType<CaptchaRejectedException>(exception);
    }

    private static CaptchaProviderEntry Entry(string type, string? siteKey) =>
        new() { Type = type, Secret = "secret", SiteKey = siteKey };

    private static Task<CaptchaValidationResult> Validate(
        DefaultCaptchaGuard guard,
        HttpContext context,
        CaptchaRequirements? requirements = null,
        CaptchaGuardOptions? options = null) =>
        guard.ValidateAsync(context, requirements ?? new CaptchaRequirements(), options, TestContext.Current.CancellationToken);

    private static Task Ensure(DefaultCaptchaGuard guard, HttpContext context) =>
        guard.EnsureAsync(context, new CaptchaRequirements(), options: null, TestContext.Current.CancellationToken);

    private static CaptchaVerificationResult Verification(bool success = true, string? action = null) =>
        new()
        {
            Success = success,
            ProviderName = "Default",
            ProviderType = CaptchaProviderTypes.Recaptcha,
            Action = action,
        };

    private static DefaultHttpContext Context(params (string Name, string Value)[] headers)
    {
        var context = new DefaultHttpContext();

        foreach (var (name, value) in headers)
        {
            context.Request.Headers[name] = value;
        }

        return context;
    }

    private static DefaultCaptchaGuard Guard(
        out StubVerifier verifier,
        string? defaultProvider = "Default",
        Dictionary<string, CaptchaProviderEntry>? entries = null)
    {
        verifier = new StubVerifier();

        var options = new CaptchaOptions { DefaultProvider = defaultProvider };

        foreach (var (name, entry) in entries ?? new Dictionary<string, CaptchaProviderEntry>
        {
            ["Default"] = Entry(CaptchaProviderTypes.Recaptcha, "key-default"),
            ["Mobile"] = Entry(CaptchaProviderTypes.Recaptcha, "key-mobile"),
            ["AdminPortal"] = Entry(CaptchaProviderTypes.Recaptcha, "key-admin"),
            ["International"] = Entry(CaptchaProviderTypes.Turnstile, "key-intl"),
        })
        {
            options.Providers[name] = entry;
        }

        var monitor = new StaticOptionsMonitor(options);

        var registry = new ConfigAwareCaptchaProviderRegistry(
            monitor,
            [new StubFactory(CaptchaProviderTypes.Recaptcha), new StubFactory(CaptchaProviderTypes.Turnstile)]);

        return new DefaultCaptchaGuard(
            registry,
            verifier,
            new DefaultCaptchaRequirementValidator(),
            monitor,
            NullLogger<DefaultCaptchaGuard>.Instance);
    }

    /// <summary>
    /// Builds providers of one type, so the guard's resolution can be tested without a network.
    /// </summary>
    private sealed class StubFactory(string type) : ICaptchaProviderFactory
    {
        public string GetProviderType() => type;

        public ICaptchaProvider Create(string providerName, CaptchaProviderEntry entry) =>
            new StubProvider(providerName, entry.Type, entry.SiteKey);
    }

    private sealed class StubProvider(string name, string type, string? siteKey) : ICaptchaProvider
    {
        public string GetProviderName() => name;

        public string GetProviderType() => type;

        public string? GetSiteKey() => siteKey;

        public Task<CaptchaVerificationResult> VerifyAsync(CaptchaVerificationInput input, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CaptchaVerificationResult { Success = true, ProviderName = name, ProviderType = type });
    }

    private sealed class StubVerifier : ICaptchaVerifier
    {
        public int Calls { get; private set; }

        public string? LastProvider { get; private set; }

        public CaptchaVerificationInput? LastInput { get; private set; }

        public CaptchaVerificationResult Result { get; set; } = Verification();

        public CaptchaException? Throws { get; set; }

        public Task<CaptchaVerificationResult> VerifyAsync(string providerName, CaptchaVerificationInput input, CancellationToken cancellationToken = default)
        {
            this.Calls++;
            this.LastProvider = providerName;
            this.LastInput = input;

            return this.Throws is not null
                ? Task.FromException<CaptchaVerificationResult>(this.Throws)
                : Task.FromResult(this.Result);
        }
    }

    private sealed class StaticOptionsMonitor(CaptchaOptions value) : IOptionsMonitor<CaptchaOptions>
    {
        public CaptchaOptions CurrentValue => value;

        public CaptchaOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<CaptchaOptions, string?> listener) => null;
    }
}
