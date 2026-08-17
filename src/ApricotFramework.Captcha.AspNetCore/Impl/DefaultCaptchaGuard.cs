using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.Captcha.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Captcha.AspNetCore.Impl;

/// <summary>
/// The default guard, which reads the challenge from the request headers and judges it
/// </summary>
public class DefaultCaptchaGuard : ICaptchaGuard
{
    /// <summary>
    /// Where this request's provider ruling is kept, so one token is redeemed once
    /// </summary>
    private static readonly object RulingCacheKey = new();

    /// <summary>
    /// The registry to resolve instances from
    /// </summary>
    protected ICaptchaProviderRegistry Registry { get; }

    /// <summary>
    /// The verifier that asks the provider to rule
    /// </summary>
    protected ICaptchaVerifier Verifier { get; }

    /// <summary>
    /// The validator that judges a ruling against the endpoint's requirements
    /// </summary>
    protected ICaptchaRequirementValidator Validator { get; }

    /// <summary>
    /// The configured providers and the default among them
    /// </summary>
    protected IOptionsMonitor<CaptchaOptions> OptionsMonitor { get; }

    /// <summary>
    /// The log to record decisions on
    /// </summary>
    protected ILogger<DefaultCaptchaGuard> Logger { get; }

    /// <summary>
    /// Creates a new instance of the guard service
    /// </summary>
    /// <param name="registry">The registry to resolve instances from</param>
    /// <param name="verifier">The verifier that asks the provider to rule</param>
    /// <param name="validator">The validator that judges the ruling</param>
    /// <param name="optionsMonitor">The configured providers</param>
    /// <param name="logger">The log to record decisions on</param>
    public DefaultCaptchaGuard(
        ICaptchaProviderRegistry registry,
        ICaptchaVerifier verifier,
        ICaptchaRequirementValidator validator,
        IOptionsMonitor<CaptchaOptions> optionsMonitor,
        ILogger<DefaultCaptchaGuard> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(verifier);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        this.Registry = registry;
        this.Verifier = verifier;
        this.Validator = validator;
        this.OptionsMonitor = optionsMonitor;
        this.Logger = logger;
    }

    /// <inheritdoc />
    public virtual async Task<CaptchaValidationResult> ValidateAsync(
        HttpContext httpContext,
        CaptchaRequirements requirements,
        CaptchaGuardOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(requirements);

        // what the client says it solved; neither header selects an instance on its own
        var declaredType = LastHeader(httpContext, CaptchaHeaders.Type);
        var declaredSiteKey = LastHeader(httpContext, CaptchaHeaders.SiteKey);

        var resolution = this.ResolveProvider(options?.Provider, declaredType, declaredSiteKey);

        if (resolution.Reason is not null)
        {
            return this.Reject(resolution.Reason, resolution.Provider, declaredType, declaredSiteKey);
        }

        var provider = resolution.Provider!;

        var response = LastHeader(httpContext, CaptchaHeaders.Response);

        if (string.IsNullOrWhiteSpace(response))
        {
            return this.Reject(CaptchaRejectionReasons.MissingResponse, provider, declaredType, declaredSiteKey);
        }

        var verification = await this
            .RuleAsync(httpContext, provider, response, declaredSiteKey, cancellationToken)
            .ConfigureAwait(false);

        var result = await this.Validator
            .ValidateAsync(requirements, verification, cancellationToken)
            .ConfigureAwait(false);

        var providerName = provider.GetProviderName();
        var providerType = provider.GetProviderType();

        if (result.Valid)
        {
            CaptchaLog.Accepted(this.Logger, providerName);
        }
        else
        {
            CaptchaLog.Rejected(this.Logger, result.Reason, providerName, providerType, declaredSiteKey);
        }

        return result;
    }

    /// <inheritdoc />
    public virtual async Task EnsureAsync(
        HttpContext httpContext,
        CaptchaRequirements requirements,
        CaptchaGuardOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var result = await this
            .ValidateAsync(httpContext, requirements, options, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Valid)
        {
            throw CaptchaRejectedException.ForReason(
                result.Reason,
                options?.Provider,
                result.ProviderType,
                result.Errors);
        }
    }

    /// <summary>
    /// Reads the last non-empty value of a header
    /// </summary>
    /// <param name="httpContext">The request to read from</param>
    /// <param name="name">The header name</param>
    /// <returns>The value, or null when the header is absent or blank.</returns>
    protected static string? LastHeader(HttpContext httpContext, string name)
    {
        // last wins, so a header appended by a proxy overrides one the client set
        return httpContext.Request.Headers.TryGetValue(name, out var values)
            ? values.LastOrDefault(static value => !string.IsNullOrWhiteSpace(value))
            : null;
    }

    /// <summary>
    /// Decides which configured instance verifies this request
    /// </summary>
    /// <param name="pinnedProvider">The instance the endpoint pinned, if any</param>
    /// <param name="declaredType">The provider type the request declared, if any</param>
    /// <param name="declaredSiteKey">The site key the request declared, if any</param>
    /// <returns>The instance, or the reason no single one could be chosen.</returns>
    protected virtual ProviderResolution ResolveProvider(string? pinnedProvider, string? declaredType, string? declaredSiteKey)
    {
        // an endpoint that pinned an instance always gets it, whatever the request declared
        if (!string.IsNullOrWhiteSpace(pinnedProvider))
        {
            var pinned = this.Registry.Find(pinnedProvider)
                ?? throw new CaptchaException($"The captcha provider '{pinnedProvider}' pinned by this endpoint is not configured.", pinnedProvider);

            return this.Revalidate(pinned, declaredType, declaredSiteKey);
        }

        var settings = this.OptionsMonitor.CurrentValue;

        // nothing to narrow by, so the default is the only candidate there could be
        if (string.IsNullOrWhiteSpace(declaredType) && string.IsNullOrWhiteSpace(declaredSiteKey))
        {
            return this.FindDefault(settings);
        }

        var candidates = this.Registry.Match(declaredType, declaredSiteKey);

        if (candidates.Count == 0)
        {
            return ProviderResolution.Failed(CaptchaRejectionReasons.UnknownProvider);
        }

        if (candidates.Count == 1)
        {
            return this.Resolved(candidates[0], declaredType, declaredSiteKey);
        }

        // several match, which is what a shared development site key looks like. Preferring the
        // default keeps adding an instance from breaking clients that only send a type; a client
        // wanting one of the others narrows it by sending its site key.
        var preferred = candidates.FirstOrDefault(
            candidate => string.Equals(candidate, settings.DefaultProvider, StringComparison.OrdinalIgnoreCase));

        return preferred is null
            ? ProviderResolution.Failed(CaptchaRejectionReasons.AmbiguousProvider)
            : this.Resolved(preferred, declaredType, declaredSiteKey);
    }

    /// <summary>
    /// Confirms a pinned instance is the one the request described
    /// </summary>
    /// <param name="provider">The pinned instance</param>
    /// <param name="declaredType">The provider type the request declared, if any</param>
    /// <param name="declaredSiteKey">The site key the request declared, if any</param>
    /// <returns>The instance, or a mismatch.</returns>
    protected virtual ProviderResolution Revalidate(ICaptchaProvider provider, string? declaredType, string? declaredSiteKey)
    {
        // failing here spares a doomed round trip and says why, where the provider would only ever
        // answer that the token was invalid
        if (!string.IsNullOrWhiteSpace(declaredType)
            && !string.Equals(declaredType, provider.GetProviderType(), StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResolution.Failed(CaptchaRejectionReasons.ProviderMismatch, provider);
        }

        // only comparable when the instance declares a key of its own
        var configuredSiteKey = provider.GetSiteKey();

        if (!string.IsNullOrWhiteSpace(declaredSiteKey)
            && !string.IsNullOrWhiteSpace(configuredSiteKey)
            && !string.Equals(declaredSiteKey, configuredSiteKey, StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResolution.Failed(CaptchaRejectionReasons.ProviderMismatch, provider);
        }

        return ProviderResolution.Succeeded(provider);
    }

    private ProviderResolution FindDefault(CaptchaOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DefaultProvider))
        {
            return ProviderResolution.Failed(CaptchaRejectionReasons.UnknownProvider);
        }

        var provider = this.Registry.Find(settings.DefaultProvider);

        return provider is null
            ? ProviderResolution.Failed(CaptchaRejectionReasons.UnknownProvider)
            : ProviderResolution.Succeeded(provider);
    }

    private ProviderResolution Resolved(string providerName, string? declaredType, string? declaredSiteKey)
    {
        var provider = this.Registry.Find(providerName);

        if (provider is null)
        {
            return ProviderResolution.Failed(CaptchaRejectionReasons.UnknownProvider);
        }

        CaptchaLog.Resolved(this.Logger, providerName, declaredType, declaredSiteKey);

        return ProviderResolution.Succeeded(provider);
    }

    private async Task<CaptchaVerificationResult> RuleAsync(
        HttpContext httpContext,
        ICaptchaProvider provider,
        string response,
        string? declaredSiteKey,
        CancellationToken cancellationToken)
    {
        var providerName = provider.GetProviderName();

        // tokens are single use, so verifying twice in one request would have the provider reject
        // the second attempt as a duplicate. Reusing the ruling lets a controller-level and an
        // action-level requirement both apply against one round trip.
        if (httpContext.Items.TryGetValue(RulingCacheKey, out var cached)
            && cached is CachedRuling ruling
            && string.Equals(ruling.Provider, providerName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ruling.Response, response, StringComparison.Ordinal))
        {
            CaptchaLog.ReusedRuling(this.Logger, providerName);

            return ruling.Result;
        }

        var input = new CaptchaVerificationInput
        {
            Response = response,
            RemoteIp = httpContext.Connection.RemoteIpAddress?.ToString(),
            ClaimedSiteKey = declaredSiteKey,
        };

        var verification = await this.Verifier
            .VerifyAsync(providerName, input, cancellationToken)
            .ConfigureAwait(false);

        httpContext.Items[RulingCacheKey] = new CachedRuling(providerName, response, verification);

        return verification;
    }

    private CaptchaValidationResult Reject(string reason, ICaptchaProvider? provider, string? declaredType, string? declaredSiteKey)
    {
        // the resolved type where there is one, so a client that sent no type still learns what
        // judged it; the instance name reaches the log only
        var providerType = provider?.GetProviderType() ?? declaredType;
        var providerName = provider?.GetProviderName();

        CaptchaLog.Rejected(this.Logger, reason, providerName, providerType, declaredSiteKey);

        return CaptchaValidationResult.Rejected(reason, errors: null, providerType);
    }

    /// <summary>
    /// The outcome of deciding which instance verifies a request
    /// </summary>
    /// <param name="Provider">The chosen instance, where one was chosen</param>
    /// <param name="Reason">Why no single instance could be chosen, or null on success</param>
    protected sealed record ProviderResolution(ICaptchaProvider? Provider, string? Reason)
    {
        /// <summary>
        /// Creates an outcome naming the instance that will verify
        /// </summary>
        /// <param name="provider">The chosen instance</param>
        /// <returns>The successful outcome.</returns>
        public static ProviderResolution Succeeded(ICaptchaProvider provider)
        {
            return new ProviderResolution(provider, null);
        }

        /// <summary>
        /// Creates an outcome explaining why nothing will verify
        /// </summary>
        /// <param name="reason">The deciding reason</param>
        /// <param name="provider">The instance involved, where the failure names one</param>
        /// <returns>The failed outcome.</returns>
        public static ProviderResolution Failed(string reason, ICaptchaProvider? provider = null)
        {
            return new ProviderResolution(provider, reason);
        }
    }

    private sealed record CachedRuling(string Provider, string Response, CaptchaVerificationResult Result);
}
