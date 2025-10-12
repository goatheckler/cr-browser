using System.Net;
using Xunit;

public class NotFoundTests
{
    [Fact]
    public async Task Unknown_Repository_Should_Return_404_Or_503()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/images/someuser/nonexistentrepo/tags");
        Assert.True(
            resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected NotFound (404) or ServiceUnavailable (503), but got {resp.StatusCode}"
        );
    }
}
