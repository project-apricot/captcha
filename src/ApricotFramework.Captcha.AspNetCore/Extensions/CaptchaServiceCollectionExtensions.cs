using ApricotFramework.Captcha.AspNetCore.Impl;
using ApricotFramework.Captcha.AspNetCore.Options;
using ApricotFramework.Captcha.AspNetCore.Providers.HCaptcha;
using ApricotFramework.Captcha.AspNetCore.Providers.Recaptcha;
using ApricotFramework.Captcha.AspNetCore.Providers.Turnstile;
using ApricotFramework.Captcha.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Captcha.AspNetCore.Extensions;

/// <summary>
/// The extensions for captcha
/// </summary>
public static class CaptchaServiceCollectionExtensions
{
    /// <summary>
    /// Adds captcha verification and the built-in provider types
    /// </summary>
    /// <param name="services">The services</param>
    /// <param name="configuration">The configuration supplying the <c>Captcha</c> section</param>
    /// <returns>The same collection, so calls chain.</returns>
    /// <remarks>
    /// Provider instances come from configuration, not from code, so this is the only call a host
    /// needs. Registering a factory of your own adds a provider type the configuration can then
    /// declare instances of.
    /// </remarks>
    public static IServiceCollection AddCaptcha(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddLogging();
        services.AddHttpClient();

        services.AddOptions<CaptchaOptions>()
            .Bind(configuration.GetSection(CaptchaOptions.SectionName))
            .ValidateOnStart();

        // a deployment mistake caught here rather than reported to a visitor as a failed captcha
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<CaptchaOptions>, CaptchaOptionsValidator>());

        services.AddCaptchaProviderFactory<RecaptchaProviderFactory>(CaptchaHttpClients.Recaptcha);
        services.AddCaptchaProviderFactory<HCaptchaProviderFactory>(CaptchaHttpClients.HCaptcha);
        services.AddCaptchaProviderFactory<TurnstileProviderFactory>(CaptchaHttpClients.Turnstile);

        services.TryAddSingleton<ICaptchaProviderRegistry, ConfigAwareCaptchaProviderRegistry>();
        services.TryAddSingleton<ICaptchaVerifier, DefaultCaptchaVerifier>();
        services.TryAddSingleton<ICaptchaRequirementValidator, DefaultCaptchaRequirementValidator>();
        services.TryAddSingleton<ICaptchaGuard, DefaultCaptchaGuard>();

        return services;
    }

    /// <summary>
    /// Adds a provider type the configuration can declare instances of
    /// </summary>
    /// <typeparam name="TFactory">The factory building instances of that type</typeparam>
    /// <param name="services">The services</param>
    /// <param name="httpClientName">The client instances of this type verify with, if it needs one</param>
    /// <returns>The same collection, so calls chain.</returns>
    /// <remarks>
    /// Deduplicated by implementation type, so adding the same factory twice leaves one. A factory
    /// added after a built-in one for the same type replaces it, which is how a host substitutes its
    /// own implementation of a provider this library ships.
    /// </remarks>
    public static IServiceCollection AddCaptchaProviderFactory<TFactory>(this IServiceCollection services, string? httpClientName = null)
        where TFactory : class, ICaptchaProviderFactory
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!string.IsNullOrWhiteSpace(httpClientName))
        {
            // the framework default is 100 seconds, long enough for a slow provider to hold a
            // request open until it becomes an availability problem of its own
            services.AddHttpClient(httpClientName)
                .ConfigureHttpClient(static (provider, client) =>
                    client.Timeout = provider.GetRequiredService<IOptions<CaptchaOptions>>().Value.VerificationTimeout);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICaptchaProviderFactory, TFactory>());

        return services;
    }
}
