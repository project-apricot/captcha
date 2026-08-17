using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha;

/// <summary>
/// Rules on a challenge with whichever provider was named.
/// </summary>
public interface ICaptchaVerifier
{
    /// <summary>
    /// Resolves the named provider and asks it to rule.
    /// </summary>
    /// <param name="providerName">The provider name matched ordinary.</param>
    /// <param name="input">The token and its context.</param>
    /// <param name="cancellationToken">Cancels the call to the provider.</param>
    /// <returns>The provider's ruling.</returns>
    /// <exception cref="Exceptions.CaptchaRejectedException">No provider answers to that name.</exception>
    /// <exception cref="Exceptions.CaptchaException">The provider could not be reached or understood.</exception>
    Task<CaptchaVerificationResult> VerifyAsync(string providerName, CaptchaVerificationInput input, CancellationToken cancellationToken = default);
}
