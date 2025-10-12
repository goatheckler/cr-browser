using System.Net;
using System.Net.Http.Json;
using CrBrowser.Api;

namespace CrBrowser.Tests.Contract;

public class LegacyEndpointContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public LegacyEndpointContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LegacyEndpoint_DefaultsToGhcrRegistry()
    {
        var legacyResponse = await _client.GetAsync("/api/images/stefanprodan/podinfo/tags");
        var ghcrResponse = await _client.GetAsync("/api/registries/ghcr/stefanprodan/podinfo/tags");
        
        if (legacyResponse.IsSuccessStatusCode && ghcrResponse.IsSuccessStatusCode)
        {
            var legacyContent = await legacyResponse.Content.ReadAsStringAsync();
            var ghcrContent = await ghcrResponse.Content.ReadAsStringAsync();
            
            Assert.Equal(ghcrContent, legacyContent);
        }
        else
        {
            Assert.Equal(legacyResponse.StatusCode, ghcrResponse.StatusCode);
        }
    }

    [Fact]
    public async Task LegacyEndpoint_HandlesInvalidFormat()
    {
        var response = await _client.GetAsync("/api/images/Invalid-Owner/repo/tags");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.NotNull(error.Code);
        Assert.NotNull(error.Message);
    }
}
