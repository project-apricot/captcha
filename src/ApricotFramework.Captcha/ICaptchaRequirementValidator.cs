using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha;

/// <summary>
/// Judges a provider's ruling against what an endpoint demands.
/// </summary>
public interface ICaptchaRequirementValidator
{
    /// <summary>
    /// Applies an endpoint's requirements to a ruling.
    /// </summary>
    /// <param name="requirements">What the endpoint demands.</param>
    /// <param name="verificationResult">What the provider ruled.</param>
    /// <param name="cancellationToken">Cancels the check for implementations that consult a service.</param>
    /// <returns>The decision and the reason for it.</returns>
    ValueTask<CaptchaValidationResult> ValidateAsync(CaptchaRequirements requirements, CaptchaVerificationResult verificationResult, CancellationToken cancellationToken = default);
}
