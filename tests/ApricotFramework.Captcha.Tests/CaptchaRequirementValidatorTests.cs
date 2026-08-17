using ApricotFramework.Captcha.Impl;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Tests;

public class CaptchaRequirementValidatorTests
{
    private readonly DefaultCaptchaRequirementValidator validator = new();

    [Fact]
    public async Task ValidateAsync_ProviderRejected_ReturnsNotVerified()
    {
        var result = await this.ValidateAsync(new CaptchaRequirements(), Result(success: false));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.NotVerified, result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_UnspecifiedPolicyAndNoScore_Accepts()
    {
        var result = await this.ValidateAsync(new CaptchaRequirements(), Result(score: null));

        Assert.True(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.Ok, result.Reason);
    }

    // The predecessor compared `null < required`, which is false, so a tier silently passed unscored.
    [Theory]
    [InlineData(CaptchaValidationPolicy.Low, null)]
    [InlineData(CaptchaValidationPolicy.Medium, null)]
    [InlineData(CaptchaValidationPolicy.High, null)]
    [InlineData(CaptchaValidationPolicy.Unspecified, 0.9)]
    public async Task ValidateAsync_ScoreIntentButProviderHasNoScore_ReturnsScoreUnavailable(
        CaptchaValidationPolicy policy,
        double? minScore)
    {
        var requirements = new CaptchaRequirements { Policy = policy, MinScore = minScore };

        var result = await this.ValidateAsync(requirements, Result(score: null));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.ScoreUnavailable, result.Reason);
    }

    [Theory]
    [InlineData(CaptchaValidationPolicy.Low, 0.2, true)]
    [InlineData(CaptchaValidationPolicy.Low, 0.19, false)]
    [InlineData(CaptchaValidationPolicy.Medium, 0.5, true)]
    [InlineData(CaptchaValidationPolicy.Medium, 0.49, false)]
    [InlineData(CaptchaValidationPolicy.High, 0.85, true)]
    [InlineData(CaptchaValidationPolicy.High, 0.84, false)]
    public async Task ValidateAsync_NamedTier_AppliesItsThreshold(
        CaptchaValidationPolicy policy,
        double score,
        bool expected)
    {
        var result = await this.ValidateAsync(new CaptchaRequirements { Policy = policy }, Result(score: score));

        Assert.Equal(expected, result.Valid);

        if (!expected)
        {
            Assert.Equal(CaptchaRejectionReasons.LowScore, result.Reason);
        }
    }

    // Nobody asked for a score, but a provider that volunteers 0.1 is still telling us something.
    [Theory]
    [InlineData(0.5, true)]
    [InlineData(0.49, false)]
    public async Task ValidateAsync_UnspecifiedPolicyWithAScore_AppliesTheMediumThreshold(double score, bool expected)
    {
        var result = await this.ValidateAsync(new CaptchaRequirements(), Result(score: score));

        Assert.Equal(expected, result.Valid);
    }

    [Fact]
    public async Task ValidateAsync_ExplicitMinScore_OverridesTheTier()
    {
        var requirements = new CaptchaRequirements
        {
            Policy = CaptchaValidationPolicy.High,
            MinScore = 0.1,
        };

        var result = await this.ValidateAsync(requirements, Result(score: 0.2));

        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateAsync_NoAllowedActions_SkipsActionCheck()
    {
        var result = await this.ValidateAsync(new CaptchaRequirements(), Result(action: "anything"));

        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateAsync_ActionRequiredButProviderHasNone_ReturnsActionUnavailable()
    {
        var requirements = new CaptchaRequirements { AllowedActions = ["sign_in"] };

        var result = await this.ValidateAsync(requirements, Result(action: null));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.ActionUnavailable, result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_ActionNotInAllowList_ReturnsActionMismatch()
    {
        var requirements = new CaptchaRequirements { AllowedActions = ["sign_in"] };

        var result = await this.ValidateAsync(requirements, Result(action: "sign_up"));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.ActionMismatch, result.Reason);
    }

    // Actions are opaque tokens the front end chooses, so case is part of the value.
    [Fact]
    public async Task ValidateAsync_ActionDifferingOnlyByCase_ReturnsActionMismatch()
    {
        var requirements = new CaptchaRequirements { AllowedActions = ["sign_in"] };

        var result = await this.ValidateAsync(requirements, Result(action: "SIGN_IN"));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.ActionMismatch, result.Reason);
    }

    // DNS names are case-insensitive, so a provider echoing Example.com must satisfy example.com.
    [Fact]
    public async Task ValidateAsync_HostDifferingOnlyByCase_Accepts()
    {
        var requirements = new CaptchaRequirements { AllowedHosts = ["example.com"] };

        var result = await this.ValidateAsync(requirements, Result(hostname: "Example.COM"));

        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateAsync_HostNotInAllowList_ReturnsHostnameMismatch()
    {
        var requirements = new CaptchaRequirements { AllowedHosts = ["example.com"] };

        var result = await this.ValidateAsync(requirements, Result(hostname: "evil.test"));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.HostnameMismatch, result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_HostRequiredButProviderHasNone_ReturnsHostnameMismatch()
    {
        var requirements = new CaptchaRequirements { AllowedHosts = ["example.com"] };

        var result = await this.ValidateAsync(requirements, Result(hostname: null));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.HostnameMismatch, result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_RejectedResult_CarriesProviderErrors()
    {
        var verification = Result(success: false) with { Errors = [CaptchaProviderErrors.TimeoutOrDuplicate] };

        var result = await this.ValidateAsync(new CaptchaRequirements(), verification);

        Assert.Equal([CaptchaProviderErrors.TimeoutOrDuplicate], result.Errors);
    }

    // The published test keys report no score and no action, and a hostname of the provider's own,
    // so an endpoint declaring any of the three could never be exercised with them.
    [Theory]
    [InlineData(CaptchaValidationPolicy.Low)]
    [InlineData(CaptchaValidationPolicy.Medium)]
    [InlineData(CaptchaValidationPolicy.High)]
    public async Task ValidateAsync_TestKeysAndNoScore_Accepts(CaptchaValidationPolicy policy)
    {
        var result = await this.ValidateAsync(
            new CaptchaRequirements { Policy = policy },
            Result(score: null, usesTestKeys: true));

        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateAsync_TestKeysAndExplicitMinScore_Accepts()
    {
        var result = await this.ValidateAsync(
            new CaptchaRequirements { MinScore = 0.9 },
            Result(score: null, usesTestKeys: true));

        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateAsync_TestKeysAndNoAction_Accepts()
    {
        var result = await this.ValidateAsync(
            new CaptchaRequirements { AllowedActions = ["sign_in"] },
            Result(action: null, usesTestKeys: true));

        Assert.True(result.Valid);
    }

    // The dummy hostname is present but is always the provider's, never the caller's.
    [Fact]
    public async Task ValidateAsync_TestKeysAndForeignHostname_Accepts()
    {
        var result = await this.ValidateAsync(
            new CaptchaRequirements { AllowedHosts = ["example.com"] },
            Result(hostname: "testkey.google.com", usesTestKeys: true));

        Assert.True(result.Valid);
    }

    // Only the unanswerable is excused. Anything the provider did answer is still judged.
    [Fact]
    public async Task ValidateAsync_TestKeysButProviderRejected_StillRejects()
    {
        var result = await this.ValidateAsync(
            new CaptchaRequirements(),
            Result(success: false, usesTestKeys: true));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.NotVerified, result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_TestKeysButScoreTooLow_StillRejects()
    {
        var result = await this.ValidateAsync(
            new CaptchaRequirements { Policy = CaptchaValidationPolicy.High },
            Result(score: 0.1, usesTestKeys: true));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.LowScore, result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_TestKeysButWrongAction_StillRejects()
    {
        var result = await this.ValidateAsync(
            new CaptchaRequirements { AllowedActions = ["sign_in"] },
            Result(action: "sign_up", usesTestKeys: true));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.ActionMismatch, result.Reason);
    }

    // Strict unless the instance says otherwise: the same inputs without the flag must fail.
    [Fact]
    public async Task ValidateAsync_WithoutTestKeys_StillEnforcesTheUnanswerable()
    {
        var requirements = new CaptchaRequirements
        {
            Policy = CaptchaValidationPolicy.High,
            AllowedActions = ["sign_in"],
            AllowedHosts = ["example.com"],
        };

        var result = await this.ValidateAsync(requirements, Result(score: null));

        Assert.False(result.Valid);
        Assert.Equal(CaptchaRejectionReasons.ScoreUnavailable, result.Reason);
    }

    private static CaptchaVerificationResult Result(
        bool success = true,
        double? score = null,
        string? action = null,
        string? hostname = null,
        bool usesTestKeys = false)
    {
        return new CaptchaVerificationResult
        {
            Success = success,
            ProviderName = "Default",
            ProviderType = CaptchaProviderTypes.Recaptcha,
            Score = score,
            Action = action,
            Hostname = hostname,
            UsesTestKeys = usesTestKeys,
        };
    }

    private async Task<CaptchaValidationResult> ValidateAsync(
        CaptchaRequirements requirements,
        CaptchaVerificationResult verification)
    {
        return await this.validator.ValidateAsync(requirements, verification, TestContext.Current.CancellationToken);
    }
}
