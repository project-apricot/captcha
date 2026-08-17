using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ApricotFramework.Captcha.Exceptions;
using ApricotFramework.Captcha.Model;

namespace ApricotFramework.Captcha.Providers;

/// <summary>
/// The shared half of a provider that verifies over the siteverify form-post protocol
/// </summary>
/// <remarks>
/// A derived provider supplies its endpoint, its secret, its error vocabulary, and how to read its
/// score. Everything else — the request shape, the transport failures, the unreadable answers — is
/// handled once here, so the three providers cannot drift in how they fail.
/// </remarks>
public abstract class SiteverifyCaptchaProviderBase : ICaptchaProvider
{
    /// <summary>
    /// Gets the provider type this instance is of
    /// </summary>
    /// <returns>A value from <see cref="CaptchaProviderTypes"/>.</returns>
    public abstract string GetProviderType();

    /// <summary>
    /// Gets the name this instance is addressed by
    /// </summary>
    /// <returns>The instance name, defaulting to the provider type for a lone instance.</returns>
    public virtual string GetProviderName()
    {
        return this.GetProviderType();
    }

    /// <summary>
    /// Gets the site key whose challenges this instance verifies
    /// </summary>
    /// <returns>The site key, or null where the host configured none.</returns>
    public virtual string? GetSiteKey()
    {
        return null;
    }

    /// <summary>
    /// Gets whether this instance is configured with the provider's published test keys
    /// </summary>
    /// <returns>True when requirements the test keys cannot answer should be treated as met.</returns>
    protected virtual bool UsesTestKeys()
    {
        return false;
    }

    /// <inheritdoc />
    public virtual async Task<CaptchaVerificationResult> VerifyAsync(CaptchaVerificationInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // the two fields every provider requires
        var body = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["secret"] = await this.GetSecretAsync(cancellationToken).ConfigureAwait(false),
            ["response"] = input.Response,
        };

        // the address is optional, and providers treat an empty one as absent anyway
        if (!string.IsNullOrWhiteSpace(input.RemoteIp))
        {
            body["remoteip"] = input.RemoteIp;
        }

        // let the provider add whatever else it accepts
        await this.ConfigureBodyAsync(body, input, cancellationToken).ConfigureAwait(false);

        var payload = await this.PostAsync(body, cancellationToken).ConfigureAwait(false);

        return new CaptchaVerificationResult
        {
            Success = payload.Success,
            ProviderName = this.GetProviderName(),
            ProviderType = this.GetProviderType(),
            ChallengeTimestamp = ParseTimestamp(payload.ChallengeTimestamp),
            Hostname = payload.Hostname,
            Score = this.NormalizeScore(payload.Success, payload.Score),
            Action = payload.Action,
            Errors = payload.ErrorCodes is null ? [] : [.. payload.ErrorCodes.Select(this.MapProviderError)],
            UsesTestKeys = this.UsesTestKeys(),
        };
    }

    /// <summary>
    /// Gets the provider's verification endpoint
    /// </summary>
    /// <returns>The absolute URI to post the verification form to.</returns>
    protected abstract Uri GetVerificationEndpoint();

    /// <summary>
    /// Gets the client to verify with, so its timeout and handlers stay the host's decision
    /// </summary>
    /// <returns>The client to post with.</returns>
    protected abstract HttpClient GetHttpClient();

    /// <summary>
    /// Gets the secret this instance authenticates the verification call with
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup</param>
    /// <returns>The secret.</returns>
    protected abstract Task<string> GetSecretAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Translates one of the provider's failure codes into this library's vocabulary
    /// </summary>
    /// <param name="providerCode">The code exactly as the provider spelled it</param>
    /// <returns>A value from <see cref="CaptchaProviderErrors"/>.</returns>
    protected abstract string MapProviderError(string providerCode);

    /// <summary>
    /// Puts the provider's score onto the shared scale, where 1.0 is the most human
    /// </summary>
    /// <param name="success">Whether the provider accepted the token</param>
    /// <param name="providerScore">The score as the provider reported it, if any</param>
    /// <returns>The normalised score, or null where the provider has no score to give.</returns>
    protected virtual double? NormalizeScore(bool success, double? providerScore)
    {
        return providerScore;
    }

    /// <summary>
    /// Adds any fields this provider accepts beyond the three every provider takes
    /// </summary>
    /// <param name="body">The form being built; secret, response and remoteip are already present</param>
    /// <param name="input">The token and its context</param>
    /// <param name="cancellationToken">Cancels the work</param>
    /// <returns>A task that completes when the body is final.</returns>
    protected virtual Task ConfigureBodyAsync(IDictionary<string, string> body, CaptchaVerificationInput input, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static DateTimeOffset? ParseTimestamp(string? value)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;
    }

    private async Task<SiteverifyResponse> PostAsync(Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        SiteverifyResponse? payload;

        try
        {
            using var content = new FormUrlEncodedContent(body);
            using var response = await this.GetHttpClient()
                .PostAsync(this.GetVerificationEndpoint(), content, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new CaptchaException(
                    $"The captcha provider answered {(int)response.StatusCode}.",
                    this.GetProviderName(),
                    this.GetProviderType());
            }

            payload = await response.Content
                .ReadFromJsonAsync<SiteverifyResponse>(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // the caller gave up, so this is not the provider's failure to report
            throw;
        }
        catch (TaskCanceledException e)
        {
            throw this.Unreachable("timed out", e);
        }
        catch (HttpRequestException e)
        {
            throw this.Unreachable("could not be reached", e);
        }
        catch (JsonException e)
        {
            throw this.Unreachable("answered with something unreadable", e);
        }

        // a 200 with an empty or null body; dereferencing it would surface as a 500 to the caller
        return payload ?? throw this.Unreachable("answered with an empty body", innerException: null);
    }

    private CaptchaException Unreachable(string what, Exception? innerException)
    {
        return new CaptchaException(
            $"The captcha provider {what}.",
            this.GetProviderName(),
            this.GetProviderType(),
            innerException);
    }
}
