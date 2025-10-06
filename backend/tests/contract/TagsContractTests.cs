using System.Net;
using System.Text.Json;
using Xunit;

public class TagsContractTests
{
    [Fact]
    public async Task Tags_Endpoint_Should_Match_Schema()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/images/stefanprodan/podinfo/tags");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("tags", out var tagsElement));
        Assert.Equal(JsonValueKind.Array, tagsElement.ValueKind);

        foreach (var tag in tagsElement.EnumerateArray())
        {
            Assert.Equal(JsonValueKind.String, tag.ValueKind);
        }
    }
}
