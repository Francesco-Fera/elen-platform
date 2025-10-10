using System.Net;
using System.Text.Json;

namespace WorkflowEngine.IntegrationTests.Helpers;

public class HttpMessageHandlerMock : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    public void SetupResponse(string url, HttpStatusCode statusCode, object? content = null)
    {
        var response = new HttpResponseMessage(statusCode);

        if (content != null)
        {
            var json = content is string str ? str : JsonSerializer.Serialize(content);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        _responses[url] = response;
    }

    public void SetupResponse(string url, HttpResponseMessage response)
    {
        _responses[url] = response;
    }

    public void SetupDefaultSuccess(object? content = null)
    {
        SetupResponse("*", HttpStatusCode.OK, content ?? new { success = true });
    }

    public void SetupDefaultFailure(HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        SetupResponse("*", statusCode, new { error = "Mock error" });
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        var url = request.RequestUri?.ToString() ?? string.Empty;

        if (_responses.TryGetValue(url, out var response))
        {
            return Task.FromResult(CloneResponse(response));
        }

        if (_responses.TryGetValue("*", out var defaultResponse))
        {
            return Task.FromResult(CloneResponse(defaultResponse));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("No mock response configured")
        });
    }

    private HttpResponseMessage CloneResponse(HttpResponseMessage original)
    {
        var clone = new HttpResponseMessage(original.StatusCode);

        if (original.Content != null)
        {
            var content = original.Content.ReadAsStringAsync().Result;
            clone.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        }

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    public void Reset()
    {
        _responses.Clear();
        _requests.Clear();
    }

    public bool WasCalled(string url)
    {
        return _requests.Any(r => r.RequestUri?.ToString() == url);
    }

    public int GetCallCount(string url)
    {
        return _requests.Count(r => r.RequestUri?.ToString() == url);
    }

    public HttpRequestMessage? GetLastRequest()
    {
        return _requests.LastOrDefault();
    }
}