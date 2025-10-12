namespace CrBrowser.Tests.Integration;

public class GcrClientTests
{
    private CrBrowser.Api.IContainerRegistryClient CreateClient()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://gcr.io/");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ghcr-browser/0.0.1");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        return new CrBrowser.Api.GcrClient(httpClient);
    }

    [Fact]
    public async Task GcrClient_Should_Fetch_Tags_For_Google_Containers()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("google-containers", "pause", 10, null);
        
        Assert.NotNull(response);
        Assert.NotNull(response.Tags);
        Assert.NotEmpty(response.Tags);
        Assert.False(response.NotFound);
        Assert.True(response.Tags.Count <= 10);
    }

    [Fact]
    public async Task GcrClient_Should_Support_Pagination()
    {
        var client = CreateClient();
        
        var firstPage = await client.ListTagsPageAsync("google-containers", "pause", 5, null);
        
        Assert.NotNull(firstPage);
        Assert.NotEmpty(firstPage.Tags);
        Assert.True(firstPage.Tags.Count <= 5);
    }

    [Fact]
    public async Task GcrClient_Should_Return_NotFound_For_Nonexistent_Image()
    {
        var client = CreateClient();
        
        var response = await client.ListTagsPageAsync("google-containers", "nonexistentimage999999", 10, null);
        
        Assert.NotNull(response);
        Assert.True(response.NotFound);
    }

    [Fact]
    public void GcrClient_Should_Have_Correct_RegistryType()
    {
        var client = CreateClient();
        
        Assert.Equal(CrBrowser.Api.RegistryType.Gcr, client.RegistryType);
    }

    [Fact]
    public void GcrClient_Should_Have_Correct_BaseUrl()
    {
        var client = CreateClient();
        
        Assert.Equal("https://gcr.io", client.BaseUrl);
    }

    [Fact]
    public void GcrClient_Should_Format_Reference_Correctly()
    {
        var client = CreateClient();
        
        var reference = client.FormatFullReference("google-containers", "pause", "latest");
        
        Assert.Equal("gcr.io/google-containers/pause:latest", reference);
    }
}
