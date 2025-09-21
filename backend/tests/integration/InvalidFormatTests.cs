using System.Net;
using Xunit;

public class InvalidFormatTests
{
    [Fact]
    public async Task Invalid_Owner_Should_Return_400_InvalidFormat()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/images/@@badowner@@/repo/tags");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
