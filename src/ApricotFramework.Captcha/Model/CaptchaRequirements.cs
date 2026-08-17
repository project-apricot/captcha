namespace ApricotFramework.Captcha.Model;

/// <summary>
/// What an endpoint demands of a challenge before it will run.
/// </summary>
/// <remarks>
/// An empty allowlist means the corresponding check does not apply. Declaring one is what turns the
/// check on, so an endpoint opts out by saying nothing rather than by setting a flag.
/// </remarks>
public sealed record CaptchaRequirements
{
    /// <summary>
    /// Gets the preset that supplies a minimum score when <see cref="MinScore"/> is not set.
    /// </summary>
    public CaptchaValidationPolicy Policy { get; init; }

    /// <summary>
    /// Gets an explicit minimum score, overriding <see cref="Policy"/>.
    /// </summary>
    public double? MinScore { get; init; }

    /// <summary>
    /// Gets the actions this endpoint accepts. Compared to ordinary, so the case must match.
    /// </summary>
    public IReadOnlyList<string> AllowedActions { get; init; } = [];

    /// <summary>
    /// Gets the hostnames this endpoint accepts. Compared case-insensitively, as DNS names are.
    /// </summary>
    public IReadOnlyList<string> AllowedHosts { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the caller expressed intent about the score.
    /// </summary>
    /// <remarks>
    /// When true a provider that reports no score fails, rather than passing a threshold, it was never
    /// measured against.
    /// </remarks>
    public bool RequiresScore => this.MinScore is not null || this.Policy != CaptchaValidationPolicy.Unspecified;
}
