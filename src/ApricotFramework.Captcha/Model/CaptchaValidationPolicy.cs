namespace ApricotFramework.Captcha.Model;

/// <summary>
/// A named minimum-score tier
/// </summary>
/// <remarks>
/// Naming a tier is what turns the score into a requirement, so a provider reporting none then fails
/// instead of passing a threshold it was never measured against.
/// </remarks>
public enum CaptchaValidationPolicy
{
    /// <summary>
    /// No score intent; applies the <see cref="Medium"/> threshold when a score is present and
    /// accepts a provider that reports none
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Requires a score, tolerating a low one
    /// </summary>
    Low = 1,

    /// <summary>
    /// Requires a score at the usual threshold
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Requires a markedly higher score
    /// </summary>
    High = 3
}
