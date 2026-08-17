using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Exceptions;

/// <summary>
/// A challenge was ruled on and did not pass
/// </summary>
/// <remarks>
/// Derives from <see cref="CaptchaException"/>, so catching that catches rejections too. The
/// <see cref="Reason"/> is what a client should switch on; the message is for logs.
/// <para>
/// Build one with <see cref="ForReason"/>. The constructors take a <em>message</em>, and a reason
/// passed to one of those would be reported as prose instead of as the decision.
/// </para>
/// </remarks>
public class CaptchaRejectedException : CaptchaException
{
    /// <summary>
    /// Creates a new instance of the rejection
    /// </summary>
    public CaptchaRejectedException()
    {
    }

    /// <summary>
    /// Creates a new instance of the rejection
    /// </summary>
    /// <param name="message">The message describing the rejection</param>
    public CaptchaRejectedException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the rejection
    /// </summary>
    /// <param name="message">The message describing the rejection</param>
    /// <param name="innerException">The underlying cause</param>
    public CaptchaRejectedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new instance of the rejection
    /// </summary>
    /// <param name="message">The message describing the rejection</param>
    /// <param name="providerName">The provider instance that ruled, where one had been resolved</param>
    /// <param name="providerType">The type of that provider</param>
    /// <param name="innerException">The underlying cause</param>
    public CaptchaRejectedException(string? message, string? providerName, string? providerType = null, Exception? innerException = null)
        : base(message, providerName, providerType, innerException)
    {
    }

    /// <summary>
    /// Gets the deciding reason, from <see cref="CaptchaRejectionReasons"/>
    /// </summary>
    public string Reason { get; private init; } = CaptchaRejectionReasons.NotVerified;

    /// <summary>
    /// Gets the provider's failure codes, where it reported any
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Creates a rejection carrying the reason a client should act on
    /// </summary>
    /// <param name="reason">The deciding reason, from <see cref="CaptchaRejectionReasons"/></param>
    /// <param name="providerName">The provider instance that ruled, where one had been resolved</param>
    /// <param name="providerType">The type of that provider</param>
    /// <param name="errors">The provider's failure codes, where it reported any</param>
    /// <returns>The rejection.</returns>
    public static CaptchaRejectedException ForReason(
        string reason,
        string? providerName = null,
        string? providerType = null,
        IReadOnlyList<string>? errors = null)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new CaptchaRejectedException($"The captcha was rejected: {reason}.", providerName, providerType)
        {
            Reason = reason,
            Errors = errors ?? [],
        };
    }
}
