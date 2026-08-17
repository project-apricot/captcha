namespace ApricotFramework.Captcha;

/// <summary>
/// Resolves configured provider instances
/// </summary>
public interface ICaptchaProviderRegistry
{
    /// <summary>
    /// Finds the instance addressed by a name
    /// </summary>
    /// <param name="providerName">The instance name matched case-insensitively</param>
    /// <returns>The instance, or null when none is configured under that name.</returns>
    ICaptchaProvider? Find(string providerName);

    /// <summary>
    /// Finds the instances a request's description could refer to
    /// </summary>
    /// <param name="providerType">The provider type the request declared, or null if it declared none</param>
    /// <param name="siteKey">The site key the request declared, or null if it declared none</param>
    /// <returns>The names of every instance matching whichever of the two were supplied.</returns>
    /// <remarks>
    /// Returns names rather than instances so that narrowing several candidates to one does not
    /// build providers that will be thrown away. Both arguments null match everything.
    /// </remarks>
    IReadOnlyList<string> Match(string? providerType, string? siteKey);
}
