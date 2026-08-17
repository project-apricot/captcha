using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Captcha.ErrorDefinitions.Extensions;

/// <summary>
/// Registers the mapper that turns captcha failures into problem+json.
/// </summary>
public static class CaptchaErrorDefinitionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the captcha exception mapper.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <returns>The same collection, so calls chain.</returns>
    /// <remarks>
    /// A mapper with no handler does nothing. The host still has to call <c>AddErrorDefinitions</c>
    /// and <c>UseExceptionHandler</c>; where the handler sits in the pipeline is its decision, not
    /// this library's.
    /// </remarks>
    public static IServiceCollection AddCaptchaErrorDefinitions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddExceptionErrorMapper<CaptchaExceptionMapper>();
    }
}
