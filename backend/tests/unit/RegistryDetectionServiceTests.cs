using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CrBrowser.Tests.Unit;

public class RegistryDetectionServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<ILogger<CrBrowser.Api.RegistryDetectionService>> _logger;
    private readonly CrBrowser.Api.RegistryDetectionService _service;

    public RegistryDetectionServiceTests()
    {
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _logger = new Mock<ILogger<CrBrowser.Api.RegistryDetectionService>>();
        _service = new CrBrowser.Api.RegistryDetectionService(_httpClientFactory.Object, _logger.Object);
    }

    [Theory]
    [InlineData("", false, "URL cannot be empty")]
    [InlineData("   ", false, "URL cannot be empty")]
    [InlineData("docker.redpanda.com", true, null)]
    [InlineData("https://docker.redpanda.com", true, null)]
    [InlineData("http://localhost:5000", true, null)]
    public void ValidateAndNormalizeUrl_Should_Validate_Input(string url, bool expectedValid, string? expectedError)
    {
        var result = _service.ValidateAndNormalizeUrl(url, out var normalizedUrl, out var error);

        Assert.Equal(expectedValid, result);
        if (!expectedValid)
        {
            Assert.Equal(expectedError, error);
            Assert.Null(normalizedUrl);
        }
        else
        {
            Assert.NotNull(normalizedUrl);
            Assert.Null(error);
        }
    }

    [Theory]
    [InlineData("docker.redpanda.com", "https://docker.redpanda.com")]
    [InlineData("https://docker.redpanda.com", "https://docker.redpanda.com")]
    [InlineData("http://localhost:5000", "http://localhost:5000")]
    [InlineData("https://gcr.io/", "https://gcr.io")]
    public void ValidateAndNormalizeUrl_Should_Normalize_Correctly(string input, string expected)
    {
        var result = _service.ValidateAndNormalizeUrl(input, out var normalizedUrl, out var error);

        Assert.True(result);
        Assert.Equal(expected, normalizedUrl);
        Assert.Null(error);
    }

    [Fact]
    public async Task DetectRegistryAsync_Should_Return_Supported_When_V2_Endpoint_Exists_With_Header()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Headers = { { "Docker-Distribution-Api-Version", "registry/2.0" } }
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _service.DetectRegistryAsync("https://docker.redpanda.com", CancellationToken.None);

        Assert.True(result.Supported);
        Assert.Equal("https://docker.redpanda.com", result.NormalizedUrl);
        Assert.Equal("registry/2.0", result.ApiVersion);
        Assert.NotNull(result.Capabilities);
        Assert.True(result.Capabilities.TagsList);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task DetectRegistryAsync_Should_Return_Supported_When_V2_Endpoint_Returns_Unauthorized_With_Header()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Headers = { { "Docker-Distribution-Api-Version", "registry/2.0" } }
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _service.DetectRegistryAsync("https://docker.redpanda.com", CancellationToken.None);

        Assert.True(result.Supported);
        Assert.Equal("registry/2.0", result.ApiVersion);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task DetectRegistryAsync_Should_Return_NotSupported_When_V2_Endpoint_NotFound()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _service.DetectRegistryAsync("https://example.com", CancellationToken.None);

        Assert.False(result.Supported);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectRegistryAsync_Should_Return_NotSupported_When_Missing_Header()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _service.DetectRegistryAsync("https://example.com", CancellationToken.None);

        Assert.False(result.Supported);
        Assert.Contains("Docker-Distribution-Api-Version", result.ErrorMessage);
    }

    [Fact]
    public async Task DetectRegistryAsync_Should_Handle_Network_Errors()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var result = await _service.DetectRegistryAsync("https://unreachable.example.com", CancellationToken.None);

        Assert.False(result.Supported);
        Assert.Contains("Unable to connect", result.ErrorMessage);
    }

    [Fact]
    public async Task DetectRegistryAsync_Should_Return_Error_For_Invalid_Url()
    {
        var result = await _service.DetectRegistryAsync("", CancellationToken.None);

        Assert.False(result.Supported);
        Assert.Equal("URL cannot be empty", result.ErrorMessage);
        Assert.Null(result.NormalizedUrl);
    }
}
