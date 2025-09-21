using System.Net;
using Xunit;

public class NotFoundTests
{
    [Fact]
    public async Task Unknown_Repository_Should_Return_404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/images/someuser/nonexistentrepo/tags"); // unchanged shape
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
