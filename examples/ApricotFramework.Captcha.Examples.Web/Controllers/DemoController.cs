using ApricotFramework.Captcha.AspNetCore.Filters;
using ApricotFramework.Captcha.Model;
using Microsoft.AspNetCore.Mvc;

namespace ApricotFramework.Captcha.Examples.Web.Controllers;

/// <summary>
/// One endpoint per way of declaring a captcha requirement
/// </summary>
[ApiController]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    /// <summary>
    /// Accepts whichever instance the request's type and site key select
    /// </summary>
    /// <returns>A trivial payload proving the action ran.</returns>
    [HttpPost("open")]
    [ValidateCaptcha]
    public IActionResult Open()
    {
        return this.Ok(new { Reached = "open" });
    }

    /// <summary>
    /// Requires a high score, so only reCAPTCHA v3 or enterprise hCaptcha can satisfy it
    /// </summary>
    /// <returns>A trivial payload proving the action ran.</returns>
    /// <remarks>
    /// A v2 or Turnstile token is rejected as <c>score_unavailable</c> rather than passing a
    /// threshold nothing measured it against.
    /// </remarks>
    [HttpPost("strict")]
    [ValidateCaptcha(Policy = CaptchaValidationPolicy.High)]
    public IActionResult Strict()
    {
        return this.Ok(new { Reached = "strict" });
    }

    /// <summary>
    /// Requires the challenge to have been solved for the <c>donate</c> action
    /// </summary>
    /// <returns>A trivial payload proving the action ran.</returns>
    [HttpPost("action")]
    [ValidateCaptcha(AllowedActions = ["donate"])]
    public IActionResult WithAction()
    {
        return this.Ok(new { Reached = "action" });
    }

    /// <summary>
    /// Pins its own instance, so this surface uses a key of its own whatever the request says
    /// </summary>
    /// <returns>A trivial payload proving the action ran.</returns>
    [HttpPost("admin")]
    [ValidateCaptcha(Provider = "AdminPortal")]
    public IActionResult Admin()
    {
        return this.Ok(new { Reached = "admin" });
    }

    /// <summary>
    /// Pins the kiosk Turnstile instance, which has a different site key from the international one
    /// </summary>
    /// <returns>A trivial payload proving the action ran.</returns>
    [HttpPost("kiosk")]
    [ValidateCaptcha(Provider = "Kiosk")]
    public IActionResult Kiosk()
    {
        return this.Ok(new { Reached = "kiosk" });
    }
}
