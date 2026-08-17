using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.ErrorDefinitions;
using ApricotFramework.ErrorDefinitions.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.Captcha.ErrorDefinitions;

/// <summary>
/// Reports captcha failures as classified errors, keeping the reason the failure already knew.
/// </summary>
/// <remarks>
/// A rejection is the caller's problem and a validation failure; an unreachable provider is ours and
/// reported as unavailable. Neither carries the exception message, which routinely names the provider
/// host and echoes back whatever the caller sent.
/// </remarks>
internal sealed class CaptchaExceptionMapper : IExceptionErrorMapper
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
    {
        // Rejection first: it derives from CaptchaException, so the general case would swallow it.
        return exception switch
        {
            CaptchaRejectedException rejected =>
            [
                Err.Validation(
                    CaptchaErrors.Rejected,
                    "The captcha was rejected.",
                    Payload(rejected)),
            ],
            CaptchaException failed =>
            [
                Err.Unavailable(
                    CaptchaErrors.VerificationFailed,
                    "The captcha could not be verified.",
                    new Dictionary<string, object?> { ["providerType"] = failed.ProviderType }),
            ],

            // Null, so every mapper registered after this one still gets its turn.
            _ => null
        };
    }

    private static Dictionary<string, object?> Payload(CaptchaRejectedException rejected)
    {
        // the provider type, never the instance name: names are internal labels the host chose, and
        // the point of resolving server side is that a client does not learn them
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["reason"] = rejected.Reason,
            ["providerType"] = rejected.ProviderType,
        };

        // Absent rather than empty, so a client can test for the key instead of for a length.
        if (rejected.Errors.Count > 0)
        {
            payload["errors"] = rejected.Errors;
        }

        return payload;
    }
}
