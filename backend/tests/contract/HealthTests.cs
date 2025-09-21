using System.Net;
using System.Text.Json;
using Xunit;

public class HealthTests
{
    [Fact]
    public async Task Health_Should_Return_Status_Ok_Field()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("status", out _));
        Assert.True(doc.RootElement.TryGetProperty("uptimeSeconds", out _));
    }
}
