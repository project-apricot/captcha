using System.Net;
using System.Text;

namespace ApricotFramework.Captcha.Tests;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder;

    private StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        this.responder = responder;
    }

    public Uri? LastRequestUri { get; private set; }

    public string? LastRequestBody { get; private set; }

    public static StubHttpMessageHandler Json(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        }));
    }

    public static StubHttpMessageHandler Status(HttpStatusCode status)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)));
    }

    public static StubHttpMessageHandler Throws(Exception exception)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromException<HttpResponseMessage>(exception));
    }

    public HttpClient CreateClient()
    {
        return new HttpClient(this, disposeHandler: false);
    }

    public IReadOnlyDictionary<string, string> ReadLastForm()
    {
        var form = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var pair in (this.LastRequestBody ?? string.Empty).Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            form[Uri.UnescapeDataString(parts[0])] = parts.Length > 1
                ? Uri.UnescapeDataString(parts[1].Replace('+', ' '))
                : string.Empty;
        }

        return form;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        this.LastRequestUri = request.RequestUri;

        if (request.Content is not null)
        {
            this.LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return await this.responder(request, cancellationToken);
    }
}
