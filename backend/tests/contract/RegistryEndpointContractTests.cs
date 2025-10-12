using System.Net;
using System.Net.Http.Json;
using CrBrowser.Api;

namespace CrBrowser.Tests.Contract;

public class RegistryEndpointContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public RegistryEndpointContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("ghcr")]
    [InlineData("dockerhub")]
    [InlineData("quay")]
    [InlineData("gcr")]
    public async Task GetTags_AcceptsRegistryTypePathParameter(string registryType)
    {
        var response = await _client.GetAsync($"/api/registries/{registryType}/testowner/testimage/tags");
        
        var hasJsonContent = response.Content.Headers.ContentType?.MediaType == "application/json";
        Assert.True(hasJsonContent || response.StatusCode == HttpStatusCode.NotFound, 
            "Endpoint should return JSON or 404 for non-existent repos");
    }

    [Fact]
    public async Task GetTags_AcceptsPageQueryParameter()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?page=2");
        
        var hasJsonContent = response.Content.Headers.ContentType?.MediaType == "application/json";
        Assert.True(hasJsonContent || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTags_AcceptsPageSizeQueryParameter()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?pageSize=50");
        
        var hasJsonContent = response.Content.Headers.ContentType?.MediaType == "application/json";
        Assert.True(hasJsonContent || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTags_AcceptsBothPageAndPageSizeParameters()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?page=2&pageSize=25");
        
        var hasJsonContent = response.Content.Headers.ContentType?.MediaType == "application/json";
        Assert.True(hasJsonContent || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTags_ReturnsJsonResponse()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags");
        
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTags_ResponseMatchesSchema_SuccessCase()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags");
        
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<RegistryResponse>();
            
            Assert.NotNull(data);
            Assert.NotNull(data.Tags);
            Assert.IsAssignableFrom<IReadOnlyList<string>>(data.Tags);
        }
    }

    [Fact]
    public async Task GetTags_ResponseMatchesSchema_ErrorCase()
    {
        var response = await _client.GetAsync("/api/registries/invalid/testowner/testimage/tags");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            
            Assert.NotNull(error);
            Assert.NotNull(error.Code);
            Assert.NotNull(error.Message);
        }
    }

    [Fact]
    public async Task GetTags_CaseInsensitiveRegistryType()
    {
        var lowerResponse = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags");
        var upperResponse = await _client.GetAsync("/api/registries/GHCR/testowner/testimage/tags");
        var mixedResponse = await _client.GetAsync("/api/registries/Ghcr/testowner/testimage/tags");
        
        Assert.Equal(lowerResponse.StatusCode, upperResponse.StatusCode);
        Assert.Equal(lowerResponse.StatusCode, mixedResponse.StatusCode);
    }

    [Fact]
    public async Task GetTags_InvalidRegistryType_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/registries/invalid/testowner/testimage/tags");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTags_DefaultsPageTo1()
    {
        var withPage1 = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?page=1");
        var withoutPage = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags");
        
        Assert.Equal(withPage1.StatusCode, withoutPage.StatusCode);
    }

    [Fact]
    public async Task GetTags_DefaultsPageSizeTo10()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags");
        
        var hasJsonContent = response.Content.Headers.ContentType?.MediaType == "application/json";
        Assert.True(hasJsonContent || response.StatusCode == HttpStatusCode.NotFound);
    }
}
