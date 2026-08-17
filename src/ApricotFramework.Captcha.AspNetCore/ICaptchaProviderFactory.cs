using ApricotFramework.Captcha.AspNetCore.Options;

namespace ApricotFramework.Captcha.AspNetCore;

/// <summary>
/// Builds provider instances of one type from their configuration
/// </summary>
/// <remarks>
/// One factory per provider type; instances come from configuration, so a host adds a provider type
/// by registering a factory and then declares as many instances of it as it needs.
/// </remarks>
public interface ICaptchaProviderFactory
{
    /// <summary>
    /// Gets the provider type this factory builds
    /// </summary>
    /// <returns>The type an entry names to be built by this factory, matched case-insensitively.</returns>
    string GetProviderType();

    /// <summary>
    /// Builds the instance an entry describes
    /// </summary>
    /// <param name="providerName">The name the instance is addressed by</param>
    /// <param name="entry">The instance's configuration</param>
    /// <returns>The provider instance.</returns>
    ICaptchaProvider Create(string providerName, CaptchaProviderEntry entry);
}
