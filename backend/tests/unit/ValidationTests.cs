using Xunit;
using GhcrBrowser.Api;

public class ValidationTests
{
    [Theory]
    [InlineData("validowner", "valid-image")] // baseline
    [InlineData("abc", "repo")] // minimal
    public void Valid_References_Should_Pass(string owner, string image)
    {
        var svc = new ValidationService();
        var ok = svc.TryParseReference(owner, image, out var reference, out var error);
        Assert.True(ok, error);
        Assert.NotNull(reference);
    }

    [Theory]
    [InlineData("bad owner", "img")] // space invalid (owner fails)
    [InlineData("UPPER", "img")] // uppercase invalid
    [InlineData("validowner", "invalid@image")] // special char in image
    [InlineData("thisonwernameiswaytoolongandexceedsmaximumlimit", "repo")] // owner too long (40+ chars)
    public void Invalid_References_Should_Fail(string owner, string image)
    {
        var svc = new ValidationService();
        var ok = svc.TryParseReference(owner, image, out var reference, out var error);
        Assert.False(ok);
        Assert.Null(reference);
        Assert.NotNull(error);
    }
}
