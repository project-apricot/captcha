using ApricotFramework.Captcha.ErrorDefinitions.Extensions;
using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.Captcha.Model;
using ApricotFramework.ErrorDefinitions;
using ApricotFramework.ErrorDefinitions.AspNetCore;
using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Captcha.ErrorDefinitions.Tests;

public class CaptchaExceptionMapperTests
{
    [Fact]
    public void Map_Rejection_ReportsValidationWithTheRejectedCode()
    {
        var errors = Map(CaptchaRejectedException.ForReason(CaptchaRejectionReasons.LowScore, "AdminPortal", CaptchaProviderTypes.Recaptcha));

        var error = Assert.Single(errors!);
        Assert.Equal(ErrorKinds.Validation, error.Kind);
        Assert.Equal(CaptchaErrors.Rejected, error.Code);
    }

    // 400, so the caller is told to try again rather than that the service is broken.
    [Fact]
    public void Map_Rejection_MapsToBadRequest()
    {
        var errors = Map(CaptchaRejectedException.ForReason(CaptchaRejectionReasons.LowScore));

        Assert.Equal(400, ErrorKindStatus.ToHttpStatusCode(errors![0].Kind));
    }

    [Fact]
    public void Map_Rejection_KeepsTheReasonInThePayload()
    {
        var errors = Map(CaptchaRejectedException.ForReason(
            CaptchaRejectionReasons.ActionMismatch,
            "AdminPortal",
            CaptchaProviderTypes.Recaptcha,
            [CaptchaProviderErrors.TimeoutOrDuplicate]));

        var payload = errors![0].Payload!;

        Assert.Equal(CaptchaRejectionReasons.ActionMismatch, payload["reason"]);
        Assert.Equal(CaptchaProviderTypes.Recaptcha, payload["providerType"]);
        Assert.Equal<IReadOnlyList<string>>(
            [CaptchaProviderErrors.TimeoutOrDuplicate],
            (IReadOnlyList<string>)payload["errors"]!);
    }

    [Fact]
    public void Map_RejectionWithoutProviderErrors_OmitsTheErrorsKey()
    {
        var errors = Map(CaptchaRejectedException.ForReason(CaptchaRejectionReasons.MissingResponse));

        Assert.DoesNotContain("errors", errors![0].Payload!.Keys);
    }

    // An outage is the service's problem, not the caller's, so it is 503 rather than 400 or 500.
    [Fact]
    public void Map_VerificationFailure_ReportsUnavailable()
    {
        var errors = Map(new CaptchaException("boom", "AdminPortal", CaptchaProviderTypes.Recaptcha));

        var error = Assert.Single(errors!);
        Assert.Equal(ErrorKinds.Unavailable, error.Kind);
        Assert.Equal(CaptchaErrors.VerificationFailed, error.Code);
        Assert.Equal(503, ErrorKindStatus.ToHttpStatusCode(error.Kind));
    }

    // The message names the provider host and echoes back whatever the caller sent.
    [Fact]
    public void Map_VerificationFailure_DoesNotLeakTheExceptionMessage()
    {
        var errors = Map(new CaptchaException("Connection to 10.0.0.1 refused; secret=hunter2", "AdminPortal", CaptchaProviderTypes.Recaptcha));

        Assert.DoesNotContain("hunter2", errors![0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("10.0.0.1", errors[0].Message, StringComparison.Ordinal);
    }

    // Rejection derives from CaptchaException, so order decides whether it is ever seen.
    [Fact]
    public void Map_Rejection_IsNotSwallowedByTheGeneralCase()
    {
        var errors = Map(CaptchaRejectedException.ForReason(CaptchaRejectionReasons.NotVerified));

        Assert.Equal(ErrorKinds.Validation, errors![0].Kind);
    }

    // Answering an exception it does not own would disable every mapper registered after it.
    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(TimeoutException))]
    public void Map_ForeignException_ReturnsNull(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Null(Map(exception));
    }

    [Fact]
    public void AddCaptchaErrorDefinitions_Always_RegistersTheMapper()
    {
        var services = new ServiceCollection();
        services.AddErrorDefinitions();
        services.AddCaptchaErrorDefinitions();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Contains(
            provider.GetServices<IExceptionErrorMapper>(),
            mapper => mapper.GetType().Name == "CaptchaExceptionMapper");
    }

    // Two libraries may each register captcha support without coordinating.
    [Fact]
    public void AddCaptchaErrorDefinitions_CalledTwice_RegistersOneMapper()
    {
        var services = new ServiceCollection();
        services.AddErrorDefinitions();
        services.AddCaptchaErrorDefinitions();
        services.AddCaptchaErrorDefinitions();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Single(
            provider.GetServices<IExceptionErrorMapper>(),
            mapper => mapper.GetType().Name == "CaptchaExceptionMapper");
    }

    // The instance name is an internal label; server-side selection exists so clients never see it.
    [Fact]
    public void Map_Rejection_DoesNotLeakTheInstanceName()
    {
        var errors = Map(CaptchaRejectedException.ForReason(
            CaptchaRejectionReasons.LowScore,
            "AdminPortal",
            CaptchaProviderTypes.Recaptcha));

        Assert.DoesNotContain("AdminPortal", errors![0].Payload!.Values.Select(value => value?.ToString()));
        Assert.DoesNotContain("AdminPortal", errors[0].Message, StringComparison.Ordinal);
    }

    private static IReadOnlyList<ErrorDefinition>? Map(Exception exception)
    {
        var services = new ServiceCollection();
        services.AddErrorDefinitions();
        services.AddCaptchaErrorDefinitions();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var mapper = provider.GetServices<IExceptionErrorMapper>()
            .Single(m => m.GetType().Name == "CaptchaExceptionMapper");

        return mapper.Map(new DefaultHttpContext(), exception);
    }
}
