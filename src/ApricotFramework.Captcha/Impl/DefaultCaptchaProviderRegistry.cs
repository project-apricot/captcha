namespace ApricotFramework.Captcha.Impl;

/// <summary>
/// The default provider registry, over a fixed set of instances
/// </summary>
/// <remarks>
/// For a host that builds its providers itself. An ASP.NET Core host uses the configuration-aware
/// registry instead, which reads instances from options as they are asked for.
/// </remarks>
public class DefaultCaptchaProviderRegistry : ICaptchaProviderRegistry
{
    /// <summary>
    /// The instances, keyed by the name they are addressed by
    /// </summary>
    protected Dictionary<string, ICaptchaProvider> Providers { get; }

    /// <summary>
    /// Creates a new instance of the provider registry
    /// </summary>
    /// <param name="providers">Every configured provider instance</param>
    /// <exception cref="ArgumentException">Two instances answer to the same name.</exception>
    public DefaultCaptchaProviderRegistry(IEnumerable<ICaptchaProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        // names are labels the host chose, so they are matched the way configuration keys are
        this.Providers = new Dictionary<string, ICaptchaProvider>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers)
        {
            var name = provider.GetProviderName();

            // silently keeping the first would make which one verifies depend on registration order
            if (!this.Providers.TryAdd(name, provider))
            {
                throw new ArgumentException($"Two captcha providers are named '{name}'.", nameof(providers));
            }
        }
    }

    /// <inheritdoc />
    public virtual ICaptchaProvider? Find(string providerName)
    {
        ArgumentNullException.ThrowIfNull(providerName);

        return this.Providers.GetValueOrDefault(providerName);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Match(string? providerType, string? siteKey)
    {
        var matches = new List<string>();

        foreach (var (name, provider) in this.Providers)
        {
            if (!Matches(providerType, provider.GetProviderType()) || !Matches(siteKey, provider.GetSiteKey()))
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
