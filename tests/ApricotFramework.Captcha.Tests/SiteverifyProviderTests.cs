using System.Net;
using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Tests;

public class SiteverifyProviderTests
{
    [Fact]
    public async Task VerifyAsync_RecaptchaV3Success_MapsEveryField()
    {
        var handler = StubHttpMessageHandler.Json(
            """
            {
              "success": true,
              "challenge_ts": "2026-08-16T12:00:00Z",
              "hostname": "example.com",
              "score": 0.9,
              "action": "sign_in"
            }
            """);

        var result = await Verify(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.True(result.Success);
        Assert.Equal("recaptcha", result.ProviderName);
        Assert.Equal(CaptchaProviderTypes.Recaptcha, result.ProviderType);
        Assert.Equal("example.com", result.Hostname);
        Assert.Equal(0.9, result.Score);
        Assert.Equal("sign_in", result.Action);
        Assert.Equal(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero), result.ChallengeTimestamp);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_RecaptchaV2Success_LeavesScoreAndActionUnset()
    {
        var handler = StubHttpMessageHandler.Json(
            """{ "success": true, "challenge_ts": "2026-08-16T12:00:00Z", "hostname": "example.com" }""");

        var result = await Verify(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.True(result.Success);
        Assert.Null(result.Score);
        Assert.Null(result.Action);
    }

    // hCaptcha reports risk, where higher is worse, so 0.2 risk is 0.8 confidence.
    [Fact]
    public async Task VerifyAsync_HCaptchaScore_IsInvertedOntoTheSharedScale()
    {
        var handler = StubHttpMessageHandler.Json(
            """{ "success": true, "hostname": "example.com", "score": 0.2 }""");

        var result = await Verify(new TestHCaptchaProvider(handler.CreateClient()));

        Assert.Equal(0.8, result.Score!.Value, precision: 10);
    }

    [Fact]
    public async Task VerifyAsync_HCaptchaWithoutScore_ReportsNoScore()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true, "hostname": "example.com" }""");

        var result = await Verify(new TestHCaptchaProvider(handler.CreateClient()));

        Assert.Null(result.Score);
    }

    // Turnstile publishes no score; the predecessor fabricated 1.0, which passed a High threshold.
    [Fact]
    public async Task VerifyAsync_TurnstileSuccess_ReportsNoScore()
    {
        var handler = StubHttpMessageHandler.Json(
            """
            {
              "success": true,
              "challenge_ts": "2026-08-16T12:00:00Z",
              "hostname": "example.com",
              "action": "donate",
              "cdata": "sessionid-123"
            }
            """);

        var result = await Verify(new TestTurnstileProvider(handler.CreateClient()));

        Assert.True(result.Success);
        Assert.Null(result.Score);
        Assert.Equal("donate", result.Action);
    }

    [Fact]
    public async Task VerifyAsync_Turnstile_PostsToCloudflareNotGoogle()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(new TestTurnstileProvider(handler.CreateClient()));

        Assert.Equal(
            new Uri("https://challenges.cloudflare.com/turnstile/v0/siteverify"),
            handler.LastRequestUri);
    }

    [Theory]
    [InlineData("missing-input-secret", CaptchaProviderErrors.MissingSecret)]
    [InlineData("invalid-input-secret", CaptchaProviderErrors.InvalidSecret)]
    [InlineData("missing-input-response", CaptchaProviderErrors.MissingResponse)]
    [InlineData("invalid-input-response", CaptchaProviderErrors.InvalidResponse)]
    [InlineData("bad-request", CaptchaProviderErrors.BadRequest)]
    [InlineData("timeout-or-duplicate", CaptchaProviderErrors.TimeoutOrDuplicate)]
    [InlineData("something-new", CaptchaProviderErrors.UnknownError)]
    public async Task VerifyAsync_RecaptchaErrorCode_MapsToSharedVocabulary(string code, string expected)
    {
        var handler = StubHttpMessageHandler.Json($$"""{ "success": false, "error-codes": ["{{code}}"] }""");

        var result = await Verify(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.False(result.Success);
        Assert.Equal([expected], result.Errors);
    }

    [Theory]
    [InlineData("expired-input-response", CaptchaProviderErrors.TimeoutOrDuplicate)]
    [InlineData("already-seen-response", CaptchaProviderErrors.TimeoutOrDuplicate)]
    [InlineData("sitekey-secret-mismatch", CaptchaProviderErrors.InvalidKeys)]
    [InlineData("invalid-remoteip", CaptchaProviderErrors.BadRequest)]
    public async Task VerifyAsync_HCaptchaErrorCode_MapsToSharedVocabulary(string code, string expected)
    {
        var handler = StubHttpMessageHandler.Json($$"""{ "success": false, "error-codes": ["{{code}}"] }""");

        var result = await Verify(new TestHCaptchaProvider(handler.CreateClient()));

        Assert.Equal([expected], result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_TurnstileInternalError_MapsToInternalError()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": false, "error-codes": ["internal-error"] }""");

        var result = await Verify(new TestTurnstileProvider(handler.CreateClient()));

        Assert.Equal([CaptchaProviderErrors.InternalError], result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_Always_SendsSecretAndResponse()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(new TestRecaptchaProvider(handler.CreateClient(), "s3cret"));

        var form = handler.ReadLastForm();
        Assert.Equal("s3cret", form["secret"]);
        Assert.Equal("token", form["response"]);
    }

    [Fact]
    public async Task VerifyAsync_WithRemoteIp_SendsIt()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(new TestRecaptchaProvider(handler.CreateClient()), new CaptchaVerificationInput
        {
            Response = "token",
            RemoteIp = "203.0.113.7",
        });

        Assert.Equal("203.0.113.7", handler.ReadLastForm()["remoteip"]);
    }

    [Fact]
    public async Task VerifyAsync_WithoutRemoteIp_OmitsIt()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.DoesNotContain("remoteip", handler.ReadLastForm().Keys);
    }

    // Only hCaptcha accepts a sitekey, and only the configured one is worth sending.
    [Fact]
    public async Task VerifyAsync_HCaptchaWithConfiguredSiteKey_SendsIt()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(new TestHCaptchaProvider(handler.CreateClient(), siteKey: "10000000-ffff-ffff-ffff-000000000001"));

        Assert.Equal("10000000-ffff-ffff-ffff-000000000001", handler.ReadLastForm()["sitekey"]);
    }

    [Fact]
    public async Task VerifyAsync_HCaptchaWithoutConfiguredSiteKey_OmitsIt()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(new TestHCaptchaProvider(handler.CreateClient()));

        Assert.DoesNotContain("sitekey", handler.ReadLastForm().Keys);
    }

    // The claimed site key is a client header and must never reach the provider as if it were verified.
    [Fact]
    public async Task VerifyAsync_ClaimedSiteKey_IsNeverForwarded()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(new TestHCaptchaProvider(handler.CreateClient()), new CaptchaVerificationInput
        {
            Response = "token",
            ClaimedSiteKey = "attacker-supplied",
        });

        Assert.DoesNotContain("attacker-supplied", handler.LastRequestBody);
    }

    [Fact]
    public async Task VerifyAsync_ProviderReturnsNonSuccessStatus_ThrowsCaptchaException()
    {
        var handler = StubHttpMessageHandler.Status(HttpStatusCode.ServiceUnavailable);

        var exception = await AssertVerifyThrows(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.Equal("recaptcha", exception.ProviderName);
    }

    // A 200 with no body used to dereference null and surface as a 500 carrying an NRE message.
    [Fact]
    public async Task VerifyAsync_ProviderReturnsEmptyBody_ThrowsCaptchaException()
    {
        var handler = StubHttpMessageHandler.Json("null");

        await AssertVerifyThrows(new TestRecaptchaProvider(handler.CreateClient()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("{ \"success\": ")]
    [InlineData("[1,2,3]")]
    public async Task VerifyAsync_ProviderReturnsUnreadableBody_ThrowsCaptchaException(string body)
    {
        var handler = StubHttpMessageHandler.Json(body);

        await AssertVerifyThrows(new TestRecaptchaProvider(handler.CreateClient()));
    }

    [Fact]
    public async Task VerifyAsync_TransportFails_ThrowsCaptchaExceptionKeepingTheCause()
    {
        var handler = StubHttpMessageHandler.Throws(new HttpRequestException("no route to host"));

        var exception = await AssertVerifyThrows(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    // A rejection is a ruling; an outage is not. Reporting the outage as a rejection is the bug.
    [Fact]
    public async Task VerifyAsync_TransportFails_DoesNotReportARejection()
    {
        var handler = StubHttpMessageHandler.Throws(new HttpRequestException("no route to host"));

        var exception = await AssertVerifyThrows(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.IsNotType<CaptchaRejectedException>(exception);
    }

    [Fact]
    public async Task VerifyAsync_CallerCancels_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");
        var provider = new TestRecaptchaProvider(handler.CreateClient());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.VerifyAsync(new CaptchaVerificationInput { Response = "token" }, cancellation.Token));
    }

    [Fact]
    public async Task VerifyAsync_MalformedTimestamp_CostsTheTimestampNotTheVerification()
    {
        var handler = StubHttpMessageHandler.Json(
            """{ "success": true, "challenge_ts": "whenever", "hostname": "example.com" }""");

        var result = await Verify(new TestRecaptchaProvider(handler.CreateClient()));

        Assert.True(result.Success);
        Assert.Null(result.ChallengeTimestamp);
    }

    [Fact]
    public async Task VerifyAsync_HugeToken_IsSentWhole()
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");
        var token = new string('t', 100_000);

        await Verify(
            new TestRecaptchaProvider(handler.CreateClient()),
            new CaptchaVerificationInput { Response = token });

        Assert.Equal(token, handler.ReadLastForm()["response"]);
    }

    [Theory]
    [InlineData("tok&en=x")]
    [InlineData("tok=en")]
    [InlineData("токен")]
    [InlineData("token with spaces")]
    public async Task VerifyAsync_TokenContainingFormDelimiters_IsEscaped(string token)
    {
        var handler = StubHttpMessageHandler.Json("""{ "success": true }""");

        await Verify(
            new TestRecaptchaProvider(handler.CreateClient()),
            new CaptchaVerificationInput { Response = token });

        Assert.Equal(token, handler.ReadLastForm()["response"]);
    }

    private static async Task<CaptchaVerificationResult> Verify(
        ICaptchaProvider provider,
        CaptchaVerificationInput? input = null)
    {
        return await provider.VerifyAsync(
            input ?? new CaptchaVerificationInput { Response = "token" },
            TestContext.Current.CancellationToken);
    }

    private static async Task<CaptchaException> AssertVerifyThrows(ICaptchaProvider provider)
    {
        return await Assert.ThrowsAnyAsync<CaptchaException>(() => Verify(provider));
    }
}
