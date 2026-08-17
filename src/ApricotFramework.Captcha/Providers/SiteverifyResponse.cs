using System.Text.Json.Serialization;

namespace ApricotFramework.Captcha.Providers;

/// <summary>
/// The site-verify document, which all three providers share the shape of.
/// </summary>
/// <remarks>
/// Turnstile omits <c>score</c> and reCAPTCHA v2 omits <c>action</c>; both simply deserialize to null.
/// <c>challenge_ts</c> stays a string so that a provider emitting an unexpected format costs a
/// timestamp rather than the whole verification.
/// </remarks>
internal sealed class SiteverifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTimestamp { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("error-codes")]
    public IReadOnlyList<string>? ErrorCodes { get; set; }
}
