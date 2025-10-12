using System.Net;
using System.Net.Http.Json;
using CrBrowser.Api;

namespace CrBrowser.Tests.Contract;

public class ErrorHandlingContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ErrorHandlingContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTags_ReturnsNotFound_ForNonexistentRepository()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/nonexistent/nonexistent/tags");
        
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.True(error.Code == "NotFound" || error.Code == "TransientUpstream");
    }

    [Fact]
    public async Task GetTags_ReturnsBadRequest_ForInvalidOwnerFormat()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/INVALID@OWNER/testimage/tags");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidFormat", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task GetTags_ReturnsBadRequest_ForInvalidImageFormat()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/INVALID@IMAGE/tags");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidFormat", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task GetTags_ReturnsBadRequest_ForUnsupportedRegistryType()
    {
        var response = await _client.GetAsync("/api/registries/unsupported/testowner/testimage/tags");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidRegistryType", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task GetTags_ReturnsBadRequest_ForInvalidPageNumber()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?page=0");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidPage", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task GetTags_ReturnsBadRequest_ForNegativePageNumber()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?page=-1");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidPage", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task GetTags_ReturnsBadRequest_ForInvalidPageSize()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?pageSize=0");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidPageSize", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task GetTags_ReturnsBadRequest_ForPageSizeExceedingMaximum()
    {
        var response = await _client.GetAsync("/api/registries/ghcr/testowner/testimage/tags?pageSize=101");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidPageSize", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task GetTags_ReturnsJsonContentType_ForAllErrorResponses()
    {
        var response = await _client.GetAsync("/api/registries/unsupported/testowner/testimage/tags");
        
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTags_LegacyEndpoint_ReturnsNotFound_ForNonexistentRepository()
    {
        var response = await _client.GetAsync("/api/images/nonexistent/nonexistent/tags");
        
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.True(error.Code == "NotFound" || error.Code == "TransientUpstream");
    }

    [Fact]
    public async Task GetTags_LegacyEndpoint_ReturnsBadRequest_ForInvalidFormat()
    {
        var response = await _client.GetAsync("/api/images/INVALID@OWNER/testimage/tags");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidFormat", error.Code);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task ErrorResponse_IncludesRequiredFields()
    {
        var response = await _client.GetAsync("/api/registries/unsupported/testowner/testimage/tags");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        
        Assert.NotNull(error);
        Assert.NotNull(error.Code);
        Assert.NotEmpty(error.Code);
        Assert.NotNull(error.Message);
        Assert.NotEmpty(error.Message);
    }
}
