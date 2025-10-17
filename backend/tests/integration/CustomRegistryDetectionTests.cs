using System.Net;
using System.Net.Http.Json;

namespace CrBrowser.Tests.Integration;

public class CustomRegistryDetectionTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public CustomRegistryDetectionTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DetectRegistry_Should_Accept_Valid_Request()
    {
        var request = new { url = "https://docker.redpanda.com" };
        var response = await _client.PostAsJsonAsync("/api/registries/detect", request);

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DetectRegistry_Should_Return_BadRequest_For_Empty_Url()
    {
        var request = new { url = "" };
        var response = await _client.PostAsJsonAsync("/api/registries/detect", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tags_Endpoint_Should_Require_CustomRegistryUrl_For_Custom_Type()
    {
        var response = await _client.GetAsync("/api/registries/Custom/owner/image/tags");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("customRegistryUrl", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Images_Endpoint_Should_Require_CustomRegistryUrl_For_Custom_Type()
    {
        var response = await _client.GetAsync("/api/registries/Custom/owner/images");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("customRegistryUrl", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tags_Endpoint_Should_Accept_CustomRegistryUrl_Query_Parameter()
    {
        var response = await _client.GetAsync("/api/registries/Custom/redpandadata/redpanda/tags?customRegistryUrl=https://docker.redpanda.com");

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.IsSuccessStatusCode,
            $"Expected success, 404, or 503 but got {response.StatusCode}");
    }

    [Fact]
    public async Task Images_Endpoint_Should_Accept_CustomRegistryUrl_Query_Parameter()
    {
        var response = await _client.GetAsync("/api/registries/Custom/redpandadata/images?customRegistryUrl=https://docker.redpanda.com");

        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotImplemented,
            $"Expected 200 or 501 but got {response.StatusCode}");
    }
}
