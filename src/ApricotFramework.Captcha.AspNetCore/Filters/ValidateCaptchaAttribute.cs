using ApricotFramework.Captcha.Model;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Captcha.AspNetCore.Filters;

/// <summary>
/// The captcha validation attribute
/// </summary>
/// <remarks>
/// Applying this to a controller and one of its actions is supported: the provider is asked once and
/// both sets of requirements are applied to that one ruling.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class ValidateCaptchaAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// The minimum-score tier
    /// </summary>
    /// <remarks>
    /// Anything but <see cref="CaptchaValidationPolicy.Unspecified"/> requires the provider to report
    /// a score, so naming a tier on an endpoint served by reCAPTCHA v2 or Turnstile rejects every
    /// request.
    /// </remarks>
    public CaptchaValidationPolicy Policy { get; set; }

    /// <summary>
    /// The explicit minimum score, overriding <see cref="Policy"/>
    /// </summary>
    public double MinScore { get; set; } = double.NaN;

    /// <summary>
    /// The set of allowed actions, leave unset to accept any
    /// </summary>
    public string[]? AllowedActions { get; set; }

    /// <summary>
    /// The set of allowed hostnames, leave unset to accept any
    /// </summary>
    public string[]? AllowedHosts { get; set; }

    /// <summary>
    /// The provider instance to verify with, overriding whatever the request describes
    /// </summary>
    /// <remarks>
    /// Names a configured instance, not a provider type. Worth setting wherever one surface needs a
    /// different key from another; left unset the request selects among the configured instances by
    /// the type and site key it declares.
    /// </remarks>
    public string? Provider { get; set; }

    /// <summary>
    /// Handles the filter execution
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <param name="next">The next action delegate</param>
    /// <returns>A task that completes when the action has run.</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // resolved per request, so the filter stays a plain attribute
        var guard = context.HttpContext.RequestServices.GetRequiredService<ICaptchaGuard>();

        var requirements = new CaptchaRequirements
        {
            Policy = this.Policy,
            MinScore = double.IsNaN(this.MinScore) ? null : this.MinScore,
            AllowedActions = this.AllowedActions ?? [],
            AllowedHosts = this.AllowedHosts ?? [],
        };

        await guard.EnsureAsync(
            context.HttpContext,
            requirements,
            new CaptchaGuardOptions { Provider = this.Provider },
            context.HttpContext.RequestAborted);

        await next();
    }
}
