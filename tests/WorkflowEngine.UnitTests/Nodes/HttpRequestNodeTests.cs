using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using WorkflowEngine.Nodes.Core;
using WorkflowEngine.Nodes.Expressions;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.UnitTests.Nodes;

public class HttpRequestNodeTests
{
    private readonly HttpRequestNode _node;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICredentialService> _credentialServiceMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public HttpRequestNodeTests()
    {
        var loggerMock = new Mock<ILogger<HttpRequestNode>>();
        var expressionEvaluator = new ExpressionEvaluator();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _credentialServiceMock = new Mock<ICredentialService>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _node = new HttpRequestNode(
            loggerMock.Object,
            expressionEvaluator,
            _httpClientFactoryMock.Object,
            _credentialServiceMock.Object);
    }

    [Fact]
    public void GetDefinition_ReturnsCorrectMetadata()
    {
        var definition = _node.GetDefinition();

        Assert.Equal("http_request", definition.Type);
        Assert.Equal("HTTP Request", definition.Name);
        Assert.Equal("Actions", definition.Category);
        Assert.NotEmpty(definition.Operations);
    }

    [Fact]
    public async Task ExecuteAsync_GetRequest_Success()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"result\":\"success\"}")
            });

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["url"] = "https://api.example.com/test",
                ["method"] = "GET"
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains("statusCode", result.OutputData);
        Assert.Equal(200, result.OutputData["statusCode"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithHeaders_IncludesHeaders()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        HttpRequestMessage? capturedRequest = null;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["url"] = "https://api.example.com/test",
                ["method"] = "GET",
                ["headers"] = new Dictionary<string, string>
                {
                    ["X-Custom-Header"] = "test-value"
                }
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains("X-Custom-Header"));
    }

    [Fact]
    public async Task ExecuteAsync_MissingUrl_ThrowsException()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["method"] = "GET"
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("url", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}