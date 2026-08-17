using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Impl;

/// <summary>
/// The default verifier, which hands the challenge to the named instance
/// </summary>
public class DefaultCaptchaVerifier : ICaptchaVerifier
{
    /// <summary>
    /// The registry to resolve instances from
    /// </summary>
    protected ICaptchaProviderRegistry Registry { get; }

    /// <summary>
    /// Creates a new instance of the captcha verifier
    /// </summary>
    /// <param name="registry">The registry to resolve instances from</param>
    public DefaultCaptchaVerifier(ICaptchaProviderRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        this.Registry = registry;
    }

    /// <inheritdoc />
    public virtual Task<CaptchaVerificationResult> VerifyAsync(string providerName, CaptchaVerificationInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var provider = string.IsNullOrWhiteSpace(providerName)
            ? null
            : this.Registry.Find(providerName);

        // the name is resolved from the request, so an unknown one is a rejection rather than a fault
        return provider is null
            ? throw CaptchaRejectedException.ForReason(CaptchaRejectionReasons.UnknownProvider)
            : provider.VerifyAsync(input, cancellationToken);
    }
}
