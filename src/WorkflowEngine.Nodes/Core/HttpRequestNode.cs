using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WorkflowEngine.Nodes.Attributes;
using WorkflowEngine.Nodes.Base;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Core;

[NodeType("http_request")]
[NodeCategory("Actions")]
public class HttpRequestNode : BaseActionNode
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialService _credentialService;

    public HttpRequestNode(
        ILogger<HttpRequestNode> logger,
        IExpressionEvaluator expressionEvaluator,
        IHttpClientFactory httpClientFactory,
        ICredentialService credentialService)
        : base(logger, expressionEvaluator)
    {
        _httpClientFactory = httpClientFactory;
        _credentialService = credentialService;
    }

    public override string Type => "http_request";
    public override string Name => "HTTP Request";

    public override NodeDefinition GetDefinition()
    {
        return new NodeDefinition
        {
            Type = Type,
            Name = Name,
            Category = Category,
            Description = "Make HTTP requests to external APIs",
            Operations = new List<NodeOperation>
            {
                new()
                {
                    Name = "request",
                    DisplayName = "Make Request",
                    Parameters = new List<NodeParameter>
                    {
                        new() { Name = "url", DisplayName = "URL", Type = "string", Required = true },
                        new()
                        {
                            Name = "method",
                            DisplayName = "Method",
                            Type = "select",
                            Required = true,
                            DefaultValue = "GET",
                            Options = new List<SelectOption>
                            {
                                new() { Value = "GET", Label = "GET" },
                                new() { Value = "POST", Label = "POST" },
                                new() { Value = "PUT", Label = "PUT" },
                                new() { Value = "DELETE", Label = "DELETE" },
                                new() { Value = "PATCH", Label = "PATCH" }
                            }
                        },
                        new() { Name = "headers", DisplayName = "Headers", Type = "json", Required = false },
                        new() { Name = "body", DisplayName = "Body", Type = "json", Required = false },
                        new()
                        {
                            Name = "authentication",
                            DisplayName = "Authentication",
                            Type = "select",
                            DefaultValue = "none",
                            Options = new List<SelectOption>
                            {
                                new() { Value = "none", Label = "None" },
                                new() { Value = "basic", Label = "Basic Auth" },
                                new() { Value = "bearer", Label = "Bearer Token" },
                                new() { Value = "oauth2", Label = "OAuth2" }
                            }
                        },
                        new() { Name = "credential_id", DisplayName = "Credential", Type = "credential", Required = false }
                    }
                }
            }
        };
    }

    protected override async Task<NodeExecutionResult> ExecuteInternalAsync(NodeExecutionContext context)
    {
        var url = GetRequiredParameter<string>(context, "url");
        var method = GetRequiredParameter<string>(context, "method");
        var headers = GetOptionalParameter<Dictionary<string, string>>(context, "headers");
        var body = GetOptionalParameter<object>(context, "body");
        var authentication = GetOptionalParameter<string>(context, "authentication", "none");
        var credentialId = GetOptionalParameter<int?>(context, "credential_id");

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        if (authentication != "none" && credentialId.HasValue)
        {
            await AddAuthenticationAsync(request, authentication, credentialId.Value, context.UserId);
        }

        if (body != null && (method == "POST" || method == "PUT" || method == "PATCH"))
        {
            var json = body is string str ? str : JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await httpClient.SendAsync(request, context.CancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(context.CancellationToken);
        object? parsedBody = null;

        try
        {
            parsedBody = JsonSerializer.Deserialize<object>(responseBody);
        }
        catch
        {
            parsedBody = responseBody;
        }

        return NodeExecutionResult.Ok(new Dictionary<string, object>
        {
            ["statusCode"] = (int)response.StatusCode,
            ["headers"] = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
            ["body"] = parsedBody ?? responseBody
        });
    }

    private async Task AddAuthenticationAsync(HttpRequestMessage request, string authType, int credentialId, Guid userId)
    {
        var credentialData = await _credentialService.GetCredentialDataAsync(credentialId, userId);

        switch (authType)
        {
            case "basic":
                var username = credentialData["username"];
                var password = credentialData["password"];
                var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
                break;

            case "bearer":
                var token = credentialData["token"];
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;

            case "oauth2":
                var accessToken = credentialData["access_token"];
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                break;
        }
    }
}