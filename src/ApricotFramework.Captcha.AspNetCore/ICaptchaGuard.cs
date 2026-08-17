using ApricotFramework.Captcha.Model;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.Captcha.AspNetCore;

/// <summary>
/// Reads a challenge off a request and judges it against what the endpoint demands.
/// </summary>
public interface ICaptchaGuard
{
    /// <summary>
    /// Verifies the request's challenge and reports the decision.
    /// </summary>
    /// <param name="httpContext">The request to read the challenge from.</param>
    /// <param name="requirements">What the endpoint demands.</param>
    /// <param name="options">Per-call overrides, or null for none.</param>
    /// <param name="cancellationToken">Cancels the call to the provider.</param>
    /// <returns>The decision and the reason for it.</returns>
    /// <exception cref="Exceptions.CaptchaException">The provider could not be reached or understood.</exception>
    Task<CaptchaValidationResult> ValidateAsync(
        HttpContext httpContext,
        CaptchaRequirements requirements,
        CaptchaGuardOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the request's challenge and throws unless it passed.
    /// </summary>
    /// <param name="httpContext">The request to read the challenge from.</param>
    /// <param name="requirements">What the endpoint demands.</param>
    /// <param name="options">Per-call overrides, or null for none.</param>
    /// <param name="cancellationToken">Cancels the call to the provider.</param>
    /// <returns>A task that completes when the challenge has passed.</returns>
    /// <exception cref="Exceptions.CaptchaRejectedException">The challenge did not pass.</exception>
    /// <exception cref="Exceptions.CaptchaException">The provider could not be reached or understood.</exception>
    Task EnsureAsync(
        HttpContext httpContext,
        CaptchaRequirements requirements,
        CaptchaGuardOptions? options = null,
        CancellationToken cancellationToken = default);
}
