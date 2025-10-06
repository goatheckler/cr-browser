using System.Net;
using System.Text.Json;
using Xunit;

public class ErrorContractTests
{
    [Fact]
    public async Task InvalidFormat_Error_Should_Match_Schema()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/images/@@bad@@/repo/tags");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("code", out var codeElement));
        Assert.Equal("InvalidFormat", codeElement.GetString());
        Assert.True(root.TryGetProperty("message", out _));
        Assert.True(root.TryGetProperty("retryable", out _));
    }

    [Fact]
    public async Task NotFound_Error_Should_Match_Schema()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/images/fake/nonexistent/tags");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("code", out var codeElement));
        Assert.Equal("NotFound", codeElement.GetString());
        Assert.True(root.TryGetProperty("message", out _));
        Assert.True(root.TryGetProperty("retryable", out _));
    }
}
