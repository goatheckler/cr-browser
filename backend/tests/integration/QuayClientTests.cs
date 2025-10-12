namespace CrBrowser.Tests.Integration;

public class QuayClientTests
{
    private CrBrowser.Api.IContainerRegistryClient CreateClient()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://quay.io/");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ghcr-browser/0.0.1");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        return new CrBrowser.Api.QuayClient(httpClient);
    }

    [Fact]
    public async Task QuayClient_Should_Fetch_Tags_For_Prometheus()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("prometheus", "prometheus", 10, null);
        
        Assert.NotNull(response);
        Assert.NotNull(response.Tags);
        Assert.NotEmpty(response.Tags);
        Assert.False(response.NotFound);
        Assert.True(response.Tags.Count <= 10);
    }

    [Fact]
    public async Task QuayClient_Should_Fetch_Tags_For_CoreOS_Etcd()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("coreos", "etcd", 10, null);
        
        Assert.NotNull(response);
        Assert.NotNull(response.Tags);
        Assert.NotEmpty(response.Tags);
        Assert.False(response.NotFound);
    }

    [Fact]
    public async Task QuayClient_Should_Support_Pagination()
    {
        var client = CreateClient();
        
        var firstPage = await client.ListTagsPageAsync("prometheus", "prometheus", 5, null);
        
        Assert.NotNull(firstPage);
        Assert.NotEmpty(firstPage.Tags);
        Assert.True(firstPage.Tags.Count <= 5);
    }

    [Fact]
    public async Task QuayClient_Should_Return_NotFound_For_Nonexistent_Image()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("prometheus", "nonexistentimage999999", 10, null);
        
        Assert.NotNull(response);
        Assert.True(response.NotFound || response.Retryable);
    }

    [Fact]
    public void QuayClient_Should_Have_Correct_RegistryType()
    {
        var client = CreateClient();
        
        Assert.Equal(CrBrowser.Api.RegistryType.Quay, client.RegistryType);
    }

    [Fact]
    public void QuayClient_Should_Have_Correct_BaseUrl()
    {
        var client = CreateClient();
        
        Assert.Equal("https://quay.io", client.BaseUrl);
    }

    [Fact]
    public void QuayClient_Should_Format_Reference_Correctly()
    {
        var client = CreateClient();
        
        var reference = client.FormatFullReference("prometheus", "prometheus", "latest");
        
        Assert.Equal("quay.io/prometheus/prometheus:latest", reference);
    }
}
