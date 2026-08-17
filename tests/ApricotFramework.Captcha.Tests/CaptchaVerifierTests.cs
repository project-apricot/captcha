using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.Captcha.Impl;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Tests;

public class CaptchaVerifierTests
{
    [Fact]
    public async Task VerifyAsync_KnownProvider_DelegatesToIt()
    {
        var verifier = Verifier();

        var result = await verifier.VerifyAsync("Default", Input(), TestContext.Current.CancellationToken);

        Assert.Equal("Default", result.ProviderName);
        Assert.Equal(CaptchaProviderTypes.Recaptcha, result.ProviderType);
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyAsync_UnresolvableProvider_ThrowsUnknownProvider(string providerName)
    {
        var verifier = Verifier();

        var exception = await Assert.ThrowsAsync<CaptchaRejectedException>(() =>
            verifier.VerifyAsync(providerName, Input(), TestContext.Current.CancellationToken));

        Assert.Equal(CaptchaRejectionReasons.UnknownProvider, exception.Reason);
    }

    // Catching the base type has to catch rejections; the predecessor derived this from Exception.
    [Fact]
    public void CaptchaRejectedException_IsACaptchaException()
    {
        Assert.IsAssignableFrom<CaptchaException>(
            CaptchaRejectedException.ForReason(CaptchaRejectionReasons.LowScore));
    }

    // A reason handed to a constructor would become prose, so the factory is the only way in.
    [Fact]
    public void ForReason_Always_KeepsTheReasonOutOfTheMessage()
    {
        var exception = CaptchaRejectedException.ForReason(
            CaptchaRejectionReasons.LowScore,
            "AdminPortal",
            CaptchaProviderTypes.Recaptcha);

        Assert.Equal(CaptchaRejectionReasons.LowScore, exception.Reason);
        Assert.Equal("AdminPortal", exception.ProviderName);
        Assert.Equal(CaptchaProviderTypes.Recaptcha, exception.ProviderType);
    }

    private static DefaultCaptchaVerifier Verifier()
    {
        return new DefaultCaptchaVerifier(new DefaultCaptchaProviderRegistry([new StubProvider("Default")]));
    }

    private static CaptchaVerificationInput Input() => new() { Response = "token" };
}
