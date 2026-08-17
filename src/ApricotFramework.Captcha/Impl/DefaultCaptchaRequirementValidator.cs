using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Impl;

/// <summary>
/// The default requirement validator
/// </summary>
/// <remarks>
/// A requirement the provider cannot answer fails rather than passing. A null score does not compare
/// as low, and a null action does not compare as mismatched, so silence would otherwise satisfy
/// every threshold the caller set.
/// </remarks>
public class DefaultCaptchaRequirementValidator : ICaptchaRequirementValidator
{
    /// <summary>
    /// The threshold for the low tier
    /// </summary>
    protected const double LowMinScore = 0.2;

    /// <summary>
    /// The threshold for the medium tier, and for a request that expressed no score intent
    /// </summary>
    protected const double MediumMinScore = 0.5;

    /// <summary>
    /// The threshold for the high tier
    /// </summary>
    protected const double HighMinScore = 0.85;

    /// <inheritdoc />
    public virtual ValueTask<CaptchaValidationResult> ValidateAsync(CaptchaRequirements requirements, CaptchaVerificationResult verificationResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(verificationResult);

        return ValueTask.FromResult(this.Validate(requirements, verificationResult));
    }

    /// <summary>
    /// Applies every requirement in turn, stopping at the first that fails
    /// </summary>
    /// <param name="requirements">What the endpoint demands</param>
    /// <param name="result">What the provider ruled</param>
    /// <returns>The decision and the reason for it.</returns>
    protected virtual CaptchaValidationResult Validate(CaptchaRequirements requirements, CaptchaVerificationResult result)
    {
        // the provider did not accept the token at all
        if (!result.Success)
        {
            return CaptchaValidationResult.Rejected(CaptchaRejectionReasons.NotVerified, result.Errors, result.ProviderType);
        }

        var scoreOutcome = this.CheckScore(requirements, result);

        if (scoreOutcome is not null)
        {
            return CaptchaValidationResult.Rejected(scoreOutcome, result.Errors, result.ProviderType);
        }

        var actionOutcome = this.CheckAction(requirements, result);

        if (actionOutcome is not null)
        {
            return CaptchaValidationResult.Rejected(actionOutcome, result.Errors, result.ProviderType);
        }

        // a test key always reports a hostname of the provider's own, never the caller's, so the
        // allowlist could never be satisfied
        // DNS names are case-insensitive, unlike the actions a front end chooses
        if (requirements.AllowedHosts.Count > 0
            && !result.UsesTestKeys
            && !requirements.AllowedHosts.Contains(result.Hostname ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            return CaptchaValidationResult.Rejected(CaptchaRejectionReasons.HostnameMismatch, result.Errors, result.ProviderType);
        }

        return CaptchaValidationResult.Accepted(result.ProviderType);
    }

    /// <summary>
    /// Applies the score requirement
    /// </summary>
    /// <param name="requirements">What the endpoint demands</param>
    /// <param name="result">What the provider ruled</param>
    /// <returns>The failing reason, or null when the score is acceptable.</returns>
    protected virtual string? CheckScore(CaptchaRequirements requirements, CaptchaVerificationResult result)
    {
        if (result.Score is null)
        {
            // without an explicit tier, a score-less provider is a legitimate choice, not a failure,
            // and a test key can never supply one however loudly the endpoint asks
            return requirements.RequiresScore && !result.UsesTestKeys
                ? CaptchaRejectionReasons.ScoreUnavailable
                : null;
        }

        var required = requirements.MinScore ?? this.GetThreshold(requirements.Policy);

        return result.Score.Value < required ? CaptchaRejectionReasons.LowScore : null;
    }

    /// <summary>
    /// Gets the minimum score a tier stands for
    /// </summary>
    /// <param name="policy">The tier the endpoint named</param>
    /// <returns>The minimum acceptable score.</returns>
    protected virtual double GetThreshold(CaptchaValidationPolicy policy)
    {
        return policy switch
        {
            CaptchaValidationPolicy.Low => LowMinScore,
            CaptchaValidationPolicy.High => HighMinScore,

            // an unspecified tier still applies the middle threshold to a score it was given
            _ => MediumMinScore,
        };
    }

    /// <summary>
    /// Applies the action requirement
    /// </summary>
    /// <param name="requirements">What the endpoint demands</param>
    /// <param name="result">What the provider ruled</param>
    /// <returns>The failing reason, or null when the action is acceptable.</returns>
    protected virtual string? CheckAction(CaptchaRequirements requirements, CaptchaVerificationResult result)
    {
        // declaring no actions is how an endpoint opts out of the check
        if (requirements.AllowedActions.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(result.Action))
        {
            // a test key reports no action, so demanding one would make the endpoint untestable
            return result.UsesTestKeys ? null : CaptchaRejectionReasons.ActionUnavailable;
        }

        // an action is an opaque token the front end chose, so its case is part of the value
        return requirements.AllowedActions.Contains(result.Action, StringComparer.Ordinal)
            ? null
            : CaptchaRejectionReasons.ActionMismatch;
    }
}
