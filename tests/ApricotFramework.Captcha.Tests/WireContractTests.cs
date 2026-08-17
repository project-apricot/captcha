using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Tests;

/// <remarks>
/// Every value here is published: it travels in a request header, a configuration key or an error
/// payload. Changing one is a breaking change for somebody's front end, so it has to break a test
/// first. The expected values are spelled out rather than read back from the constants, because a
/// test that reads the constant cannot notice the constant changing.
/// <para>
/// Provider <em>instance</em> names are deliberately absent: those are labels a host invents, never
/// sent by a client, and so not a contract at all.
/// </para>
/// </remarks>
public class WireContractTests
{
    [Fact]
    public void ProviderTypes_AreTheirPublishedTokens()
    {
        Assert.Equal("recaptcha", CaptchaProviderTypes.Recaptcha);
        Assert.Equal("hcaptcha", CaptchaProviderTypes.HCaptcha);
        Assert.Equal("turnstile", CaptchaProviderTypes.Turnstile);
    }

    [Fact]
    public void RejectionReasons_AreTheirPublishedValues()
    {
        Assert.Equal("ok", CaptchaRejectionReasons.Ok);
        Assert.Equal("unknown_provider", CaptchaRejectionReasons.UnknownProvider);
        Assert.Equal("provider_mismatch", CaptchaRejectionReasons.ProviderMismatch);
        Assert.Equal("ambiguous_provider", CaptchaRejectionReasons.AmbiguousProvider);
        Assert.Equal("missing_response", CaptchaRejectionReasons.MissingResponse);
        Assert.Equal("not_verified", CaptchaRejectionReasons.NotVerified);
        Assert.Equal("low_score", CaptchaRejectionReasons.LowScore);
        Assert.Equal("score_unavailable", CaptchaRejectionReasons.ScoreUnavailable);
        Assert.Equal("action_mismatch", CaptchaRejectionReasons.ActionMismatch);
        Assert.Equal("action_unavailable", CaptchaRejectionReasons.ActionUnavailable);
        Assert.Equal("hostname_mismatch", CaptchaRejectionReasons.HostnameMismatch);
    }

    [Fact]
    public void ProviderErrors_AreTheirPublishedValues()
    {
        Assert.Equal("missing_secret", CaptchaProviderErrors.MissingSecret);
        Assert.Equal("invalid_secret", CaptchaProviderErrors.InvalidSecret);
        Assert.Equal("missing_response", CaptchaProviderErrors.MissingResponse);
        Assert.Equal("invalid_response", CaptchaProviderErrors.InvalidResponse);
        Assert.Equal("bad_request", CaptchaProviderErrors.BadRequest);
        Assert.Equal("timeout_or_duplicate", CaptchaProviderErrors.TimeoutOrDuplicate);
        Assert.Equal("invalid_keys", CaptchaProviderErrors.InvalidKeys);
        Assert.Equal("internal_error", CaptchaProviderErrors.InternalError);
        Assert.Equal("unknown_error", CaptchaProviderErrors.UnknownError);
    }

    [Fact]
    public void ValidationPolicy_KeepsItsNumbering()
    {
        Assert.Equal(0, (int)CaptchaValidationPolicy.Unspecified);
        Assert.Equal(1, (int)CaptchaValidationPolicy.Low);
        Assert.Equal(2, (int)CaptchaValidationPolicy.Medium);
        Assert.Equal(3, (int)CaptchaValidationPolicy.High);
    }
}
