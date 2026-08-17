using Microsoft.Extensions.Logging;

namespace ApricotFramework.Captcha.AspNetCore.Impl;

/// <remarks>
/// The token never appears here. It is a bearer credential for the provider until it is redeemed,
/// and logs outlive it. Instance names do appear: they are internal labels, which is exactly why
/// they belong in a log rather than in a response.
/// </remarks>
internal static partial class CaptchaLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Captcha rejected: {Reason}. Provider {Provider}, type {ProviderType}, site key {SiteKey}.")]
    public static partial void Rejected(ILogger logger, string reason, string? provider, string? providerType, string? siteKey);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Captcha accepted by {Provider}.")]
    public static partial void Accepted(ILogger logger, string provider);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Reusing the {Provider} ruling already obtained for this request.")]
    public static partial void ReusedRuling(ILogger logger, string provider);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Resolved captcha provider {Provider} from type {ProviderType} and site key {SiteKey}.")]
    public static partial void Resolved(ILogger logger, string provider, string? providerType, string? siteKey);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Captcha provider {Provider} is configured with the provider's published test keys, "
                  + "so it verifies any token and score, action and hostname requirements are not "
                  + "enforced against it. This must not be a production configuration.")]
    public static partial void ConfiguredWithTestKeys(ILogger logger, string provider);
}
