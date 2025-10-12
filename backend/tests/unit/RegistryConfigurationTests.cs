namespace CrBrowser.Tests.Unit;

public class RegistryConfigurationTests
{
    [Fact]
    public void RegistryConfiguration_Should_Require_BaseUrl()
    {
        var config = new CrBrowser.Api.RegistryConfiguration(
            CrBrowser.Api.RegistryType.Ghcr,
            ""
        );
        
        Assert.NotNull(config);
        Assert.Equal("", config.BaseUrl);
    }

    [Theory]
    [InlineData("https://ghcr.io")]
    [InlineData("https://registry-1.docker.io")]
    [InlineData("https://quay.io")]
    public void RegistryConfiguration_Should_Accept_Valid_BaseUrl(string baseUrl)
    {
        var config = new CrBrowser.Api.RegistryConfiguration(
            CrBrowser.Api.RegistryType.Ghcr,
            baseUrl
        );
        
        Assert.NotNull(config);
        Assert.Equal(baseUrl, config.BaseUrl);
        Assert.True(Uri.TryCreate(baseUrl, UriKind.Absolute, out _), $"BaseUrl {baseUrl} should be a valid URI");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData("ftp://invalid")]
    public void RegistryConfiguration_Should_Have_Invalid_BaseUrl_For_Non_Http_Uris(string baseUrl)
    {
        var config = new CrBrowser.Api.RegistryConfiguration(
            CrBrowser.Api.RegistryType.Ghcr,
            baseUrl
        );
        
        Assert.NotNull(config);
        var isValidHttpUri = Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && 
                             (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        Assert.False(isValidHttpUri, $"BaseUrl {baseUrl} should not be a valid HTTP/HTTPS URI");
    }

    [Fact]
    public void RegistryConfiguration_AuthUrl_Should_Default_To_Null()
    {
        var config = new CrBrowser.Api.RegistryConfiguration(
            CrBrowser.Api.RegistryType.Ghcr,
            "https://ghcr.io"
        );
        
        Assert.Null(config.AuthUrl);
    }

    [Fact]
    public void RegistryConfiguration_AuthUrl_Should_Be_Settable()
    {
        var config = new CrBrowser.Api.RegistryConfiguration(
            CrBrowser.Api.RegistryType.DockerHub,
            "https://registry-1.docker.io",
            "https://auth.docker.io"
        );
        
        Assert.Equal("https://auth.docker.io", config.AuthUrl);
    }

    [Theory]
    [InlineData(CrBrowser.Api.RegistryType.Ghcr)]
    [InlineData(CrBrowser.Api.RegistryType.DockerHub)]
    [InlineData(CrBrowser.Api.RegistryType.Quay)]
    [InlineData(CrBrowser.Api.RegistryType.Gcr)]
    public void RegistryConfiguration_Should_Support_All_RegistryTypes(CrBrowser.Api.RegistryType registryType)
    {
        var config = new CrBrowser.Api.RegistryConfiguration(
            registryType,
            "https://example.com"
        );
        
        Assert.Equal(registryType, config.Type);
    }
}
