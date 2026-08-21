using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CrBrowser.Tests.Unit;

public class DockerHubClientTests
{
    private readonly Mock<ILogger<CrBrowser.Api.DockerHubClient>> _logger;

    public DockerHubClientTests()
    {
        _logger = new Mock<ILogger<CrBrowser.Api.DockerHubClient>>();
    }

    private CrBrowser.Api.DockerHubClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://registry-1.docker.io/");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
        return new CrBrowser.Api.DockerHubClient(httpClient, _logger.Object);
    }

    [Fact]
    public async Task AcquireToken_Should_Be_Cached_Per_Repository_Across_Pages()
    {
        var tokenRequests = 0;
        var handlerMock = new Mock<HttpMessageHandler>();

        // Tags endpoint: 401 first (no bearer), 200 after bearer is attached.
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.Host == "registry-1.docker.io"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
                req.Headers.Authorization is null
                    ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"tags\":[\"v1\",\"v2\"]}")
                    });

        // Token endpoint: count calls, return a fake token.
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.Host == "auth.docker.io"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref tokenRequests);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token\":\"fake-token\",\"expires_in\":300}")
                };
            });

        var client = CreateClient(handlerMock.Object);

        // Two sequential pages (as the tags endpoint does for pagination).
        var first = await client.ListTagsPageAsync("library", "nginx", 100, null, CancellationToken.None);
        var second = await client.ListTagsPageAsync("library", "nginx", 100, "v2", CancellationToken.None);

        Assert.False(first.NotFound);
        Assert.False(first.Retryable);
        Assert.True(first.Tags.Count > 0);
        Assert.True(second.Tags.Count > 0);

        // Token must be fetched exactly once across both pages.
        Assert.Equal(1, tokenRequests);
    }

    [Fact]
    public async Task AcquireToken_Should_Be_Fetched_Again_For_Different_Repository()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.Host == "registry-1.docker.io"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
                req.Headers.Authorization is null
                    ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"tags\":[\"v1\"]}")
                    });

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.Host == "auth.docker.io"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"token\":\"fake-token\",\"expires_in\":300}")
            });

        var client = CreateClient(handlerMock.Object);

        await client.ListTagsPageAsync("library", "nginx", 100, null, CancellationToken.None);
        await client.ListTagsPageAsync("library", "redis", 100, null, CancellationToken.None);

        // Token endpoint should be requested twice: once per distinct repository.
        handlerMock.Protected()
            .Verify("SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.Host == "auth.docker.io"),
                ItExpr.IsAny<CancellationToken>());
    }
}
