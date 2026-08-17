namespace ApricotFramework.Captcha.Exceptions;

/// <summary>
/// A challenge could not be ruled on at all.
/// </summary>
/// <remarks>
/// This is an infrastructure failure — the provider was unreachable, timed out, or answered with
/// something unreadable. It is deliberately distinct from a challenge that was ruled on and rejected,
/// because reporting an outage as a rejection tells the user to try a captcha that was never asked.
/// </remarks>
public class CaptchaException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptchaException"/> class.
    /// </summary>
    public CaptchaException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptchaException"/> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    public CaptchaException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptchaException"/> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public CaptchaException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptchaException"/> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="providerName">The provider that was being asked.</param>
    /// <param name="providerType">The type of that provider.</param>
    /// <param name="innerException">The underlying cause.</param>
    public CaptchaException(string? message, string? providerName, string? providerType = null, Exception? innerException = null)
        : base(message, innerException)
    {
        this.ProviderName = providerName;
        this.ProviderType = providerType;
    }

    /// <summary>
    /// Gets the provider instance that was being asked where one had been resolved.
    /// </summary>
    /// <remarks>
    /// An internal label, so it belongs in a log rather than in anything a client sees.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>
    /// Gets the type of that provider.
    /// </summary>
    public string? ProviderType { get; }
}
