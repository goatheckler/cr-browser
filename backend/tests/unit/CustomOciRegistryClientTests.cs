using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CrBrowser.Tests.Unit;

public class CustomOciRegistryClientTests
{
    private readonly Mock<ILogger<CrBrowser.Api.CustomOciRegistryClient>> _logger;

    public CustomOciRegistryClientTests()
    {
        _logger = new Mock<ILogger<CrBrowser.Api.CustomOciRegistryClient>>();
    }

    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var baseUrl = "https://docker.redpanda.com";
        var httpClient = new HttpClient();
        var client = new CrBrowser.Api.CustomOciRegistryClient(baseUrl, httpClient, _logger.Object);

        Assert.Equal(CrBrowser.Api.RegistryType.Custom, client.RegistryType);
        Assert.Equal(baseUrl, client.BaseUrl);
    }

    [Theory]
    [InlineData("https://docker.redpanda.com", "redpandadata", "redpanda", "latest", "docker.redpanda.com/redpandadata/redpanda:latest")]
    [InlineData("https://gcr.io", "my-project", "my-image", "v1.0.0", "gcr.io/my-project/my-image:v1.0.0")]
    public void FormatFullReference_Should_Return_Correct_Format(string baseUrl, string owner, string image, string tag, string expected)
    {
        var httpClient = new HttpClient();
        var client = new CrBrowser.Api.CustomOciRegistryClient(baseUrl, httpClient, _logger.Object);

        var result = client.FormatFullReference(owner, image, tag);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ListTagsPageAsync_Should_Return_Tags_From_OCI_Registry()
    {
        var baseUrl = "https://docker.redpanda.com";
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/v2/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"tags\":[\"v1.0.0\",\"v1.0.1\",\"latest\"]}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        httpClient.BaseAddress = new Uri(baseUrl);

        var client = new CrBrowser.Api.CustomOciRegistryClient(baseUrl, httpClient, _logger.Object);
        var result = await client.ListTagsPageAsync("redpandadata", "redpanda", 100, null, CancellationToken.None);

        Assert.False(result.NotFound);
        Assert.False(result.Retryable);
        Assert.Contains("v1.0.0", result.Tags);
        Assert.Contains("v1.0.1", result.Tags);
        Assert.Contains("latest", result.Tags);
    }

    [Fact]
    public async Task ListTagsPageAsync_Should_Return_NotFound_When_Repository_Does_Not_Exist()
    {
        var baseUrl = "https://docker.redpanda.com";
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
        httpClient.BaseAddress = new Uri(baseUrl);

        var client = new CrBrowser.Api.CustomOciRegistryClient(baseUrl, httpClient, _logger.Object);
        var result = await client.ListTagsPageAsync("nonexistent", "repo", 100, null, CancellationToken.None);

        Assert.True(result.NotFound);
        Assert.False(result.Retryable);
    }

    [Fact]
    public async Task ListImagesAsync_Should_Return_Empty_Response()
    {
        var baseUrl = "https://docker.redpanda.com";
        var httpClient = new HttpClient();
        var client = new CrBrowser.Api.CustomOciRegistryClient(baseUrl, httpClient, _logger.Object);

        var result = await client.ListImagesAsync("owner", 25, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Images);
        Assert.Null(result.TotalCount);
        Assert.Null(result.NextPageUrl);
    }
}
