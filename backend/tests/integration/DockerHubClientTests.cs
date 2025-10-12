namespace CrBrowser.Tests.Integration;

using Microsoft.Extensions.Logging;

public class DockerHubClientTests
{
    private CrBrowser.Api.IContainerRegistryClient CreateClient()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://registry-1.docker.io/");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ghcr-browser/0.0.1");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<CrBrowser.Api.DockerHubClient>();
        
        return new CrBrowser.Api.DockerHubClient(httpClient, logger);
    }

    [Fact]
    public async Task DockerHubClient_Should_Fetch_Tags_For_Official_Image()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("library", "nginx", 10, null);
        
        Assert.NotNull(response);
        Assert.NotNull(response.Tags);
        Assert.NotEmpty(response.Tags);
        Assert.False(response.NotFound);
        Assert.True(response.Tags.Count <= 10);
    }

    [Fact]
    public async Task DockerHubClient_Should_Fetch_Tags_For_User_Image()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("bitnami", "nginx", 10, null);
        
        Assert.NotNull(response);
        Assert.NotNull(response.Tags);
        Assert.NotEmpty(response.Tags);
        Assert.False(response.NotFound);
    }

    [Fact]
    public async Task DockerHubClient_Should_Support_Pagination()
    {
        var client = CreateClient();
        
        var firstPage = await client.ListTagsPageAsync("library", "nginx", 5, null);
        
        Assert.NotNull(firstPage);
        Assert.NotEmpty(firstPage.Tags);
        Assert.True(firstPage.Tags.Count <= 5);
    }

    [Fact]
    public async Task DockerHubClient_Should_Return_NotFound_For_Nonexistent_Image()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("library", "nonexistentimage999999", 10, null);
        
        Assert.NotNull(response);
        Assert.True(response.NotFound || response.Retryable);
    }

    [Fact]
    public void DockerHubClient_Should_Have_Correct_RegistryType()
    {
        var client = CreateClient();
        
        Assert.Equal(CrBrowser.Api.RegistryType.DockerHub, client.RegistryType);
    }

    [Fact]
    public void DockerHubClient_Should_Have_Correct_BaseUrl()
    {
        var client = CreateClient();
        
        Assert.Equal("https://registry-1.docker.io", client.BaseUrl);
    }

    [Fact]
    public void DockerHubClient_Should_Format_Official_Image_Reference_Correctly()
    {
        var client = CreateClient();
        
        var reference = client.FormatFullReference("library", "nginx", "latest");
        
        Assert.Equal("docker.io/library/nginx:latest", reference);
    }

    [Fact]
    public void DockerHubClient_Should_Format_User_Image_Reference_Correctly()
    {
        var client = CreateClient();
        
        var reference = client.FormatFullReference("bitnami", "nginx", "1.25");
        
        Assert.Equal("docker.io/bitnami/nginx:1.25", reference);
    }
}
