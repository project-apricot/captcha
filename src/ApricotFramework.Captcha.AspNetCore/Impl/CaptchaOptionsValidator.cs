using ApricotFramework.Captcha.AspNetCore.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Captcha.AspNetCore.Impl;

/// <summary>
/// Checks the configured providers before the host starts serving
/// </summary>
/// <remarks>
/// Everything here is a deployment mistake that would otherwise surface as a rejected captcha, which
/// blames the visitor for the operator's error.
/// <para>
/// Site keys and secrets are deliberately not checked for uniqueness. The providers publish a single
/// reCAPTCHA test pair, and hCaptcha's test keys share one secret, so requiring either to be unique
/// would break every development configuration. Duplicates resolve through the ordinary
/// several-candidates rule instead.
/// </para>
/// </remarks>
public class CaptchaOptionsValidator : IValidateOptions<CaptchaOptions>
{
    /// <summary>
    /// The factories able to build each provider type
    /// </summary>
    protected IReadOnlyList<ICaptchaProviderFactory> Factories { get; }

    /// <summary>
    /// The instances already warned about, since options are validated more than once per start
    /// </summary>
    private readonly HashSet<string> warned = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The log to warn about test keys on
    /// </summary>
    protected ILogger<CaptchaOptionsValidator> Logger { get; }

    /// <summary>
    /// Creates a new instance of the option validator
    /// </summary>
    /// <param name="factories">The factories able to build each provider type</param>
    /// <param name="logger">The log to warn about test keys on</param>
    public CaptchaOptionsValidator(IEnumerable<ICaptchaProviderFactory> factories, ILogger<CaptchaOptionsValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(factories);
        ArgumentNullException.ThrowIfNull(logger);

        this.Factories = [.. factories];
        this.Logger = logger;
    }

    /// <inheritdoc />
    public virtual ValidateOptionsResult Validate(string? name, CaptchaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        var knownTypes = this.Factories
            .Select(factory => factory.GetProviderType())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (providerName, entry) in options.Providers)
        {
            if (string.IsNullOrWhiteSpace(entry.Type))
            {
                failures.Add($"The captcha provider '{providerName}' does not declare a type.");
            }
            else if (!knownTypes.Contains(entry.Type))
            {
                failures.Add(
                    $"The captcha provider '{providerName}' declares the unknown type '{entry.Type}'. Known types: {string.Join(", ", knownTypes.Order(StringComparer.Ordinal))}.");
            }

            if (string.IsNullOrWhiteSpace(entry.Secret))
            {
                failures.Add($"The captcha provider '{providerName}' has no secret.");
            }

            // not a failure, because it is a legitimate development setting; loud, because it is
            // never a legitimate production one
            if (entry.UsesTestKeys && this.ShouldWarn(providerName))
            {
                CaptchaLog.ConfiguredWithTestKeys(this.Logger, providerName);
            }
        }

        // a default naming nothing would only be discovered by a request that needed it
        if (!string.IsNullOrWhiteSpace(options.DefaultProvider)
            && !options.Providers.ContainsKey(options.DefaultProvider))
        {
            failures.Add($"The default captcha provider '{options.DefaultProvider}' is not configured.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Decides whether this instance still needs warning about
    /// </summary>
    /// <param name="providerName">The instance configured with test keys</param>
    /// <returns>True, the first time an instance is seen, false afterward.</returns>
    /// <remarks>
    /// Options are validated once for startup validation and again when something first resolves
    /// them, so the warning unconditionally repeats every message and makes the log look broken.
    /// </remarks>
    protected virtual bool ShouldWarn(string providerName)
    {
        lock (this.warned)
        {
            return this.warned.Add(providerName);
        }
    }
}
