using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha;

/// <summary>
/// One configured captcha provider, able to rule on a challenge token it issued
/// </summary>
/// <remarks>
/// An instance pairs a provider type with the credentials of one site, so several instances of the
/// same type can coexist with different secrets.
/// </remarks>
public interface ICaptchaProvider
{
    /// <summary>
    /// Gets the name this instance is addressed by
    /// </summary>
    /// <returns>The instance name, which is a label the host chose and never leaves the process.</returns>
    string GetProviderName();

    /// <summary>
    /// Gets the provider type this instance is of
    /// </summary>
    /// <returns>A value from <see cref="CaptchaProviderTypes"/>.</returns>
    string GetProviderType();

    /// <summary>
    /// Gets the site key whose challenges this instance verifies
    /// </summary>
    /// <returns>The site key, or null where the host configured none.</returns>
    string? GetSiteKey();

    /// <summary>
    /// Asks the provider to rule on a challenge token
    /// </summary>
    /// <param name="input">The token and its context</param>
    /// <param name="cancellationToken">Cancels the call to the provider</param>
    /// <returns>The provider's ruling.</returns>
    /// <exception cref="Exceptions.CaptchaException">The provider could not be reached or understood.</exception>
    Task<CaptchaVerificationResult> VerifyAsync(CaptchaVerificationInput input, CancellationToken cancellationToken = default);
}