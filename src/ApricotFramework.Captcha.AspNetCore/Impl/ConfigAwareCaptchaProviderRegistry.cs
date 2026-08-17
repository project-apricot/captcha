using ApricotFramework.Captcha.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Captcha.AspNetCore.Impl;

/// <summary>
/// Resolves provider instances declared in configuration
/// </summary>
/// <remarks>
/// Matching reads the configured entries and builds nothing; only the instance actually chosen is
/// built, and it is built per call rather than cached. A provider holds a secret and a client
/// factory, so that costs nothing and means a rotated secret takes effect on the next request
/// instead of the next restart.
/// </remarks>
public class ConfigAwareCaptchaProviderRegistry : ICaptchaProviderRegistry
{
    /// <summary>
    /// The configured instances
    /// </summary>
    protected IOptionsMonitor<CaptchaOptions> OptionsMonitor { get; }

    /// <summary>
    /// The factories able to build each provider type
    /// </summary>
    protected IReadOnlyDictionary<string, ICaptchaProviderFactory> Factories { get; }

    /// <summary>
    /// Creates a new instance of the configuration-aware registry
    /// </summary>
    /// <param name="optionsMonitor">The configured instances</param>
    /// <param name="factories">The factories able to build each provider type</param>
    public ConfigAwareCaptchaProviderRegistry(IOptionsMonitor<CaptchaOptions> optionsMonitor, IEnumerable<ICaptchaProviderFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(factories);

        this.OptionsMonitor = optionsMonitor;

        // a later registration for the same type replaces an earlier one, so a host can substitute
        // its own implementation of a built-in provider
        var byType = new Dictionary<string, ICaptchaProviderFactory>(StringComparer.OrdinalIgnoreCase);

        foreach (var factory in factories)
        {
            byType[factory.GetProviderType()] = factory;
        }

        this.Factories = byType;
    }

    /// <inheritdoc />
    public virtual ICaptchaProvider? Find(string providerName)
    {
        ArgumentNullException.ThrowIfNull(providerName);

        // the configured spelling, not the caller's: an instance is named once, in configuration,
        // and that is the name that should reach a log
        var configured = this.OptionsMonitor.CurrentValue.Providers
            .FirstOrDefault(candidate => string.Equals(candidate.Key, providerName, StringComparison.OrdinalIgnoreCase));

        if (configured.Value is null)
        {
            return null;
        }

        // an entry naming a type nothing can build is a configuration error that startup validation
        // already reports, so reaching here means validation was bypassed
        return this.Factories.TryGetValue(configured.Value.Type, out var factory)
            ? factory.Create(configured.Key, configured.Value)
            : null;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Match(string? providerType, string? siteKey)
    {
        var matches = new List<string>();

        foreach (var (name, entry) in this.OptionsMonitor.CurrentValue.Providers)
        {
            if (!Matches(providerType, entry.Type) || !Matches(siteKey, entry.SiteKey))
            {
                continue;
            }

            matches.Add(name);
        }

        return matches;
    }

    /// <summary>
    /// Decides whether a value the request supplied narrows to a candidate
    /// </summary>
    /// <param name="requested">What the request declared, or null if it declared nothing</param>
    /// <param name="candidate">What the instance has configured</param>
    /// <returns>True when the request does not narrow or narrows to this candidate.</returns>
    protected static bool Matches(string? requested, string? candidate)
    {
        // a value the request did not supply cannot exclude anything
        return string.IsNullOrWhiteSpace(requested)
            || string.Equals(requested, candidate, StringComparison.OrdinalIgnoreCase);
    }
}
