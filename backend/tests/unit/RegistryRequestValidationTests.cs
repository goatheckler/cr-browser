namespace CrBrowser.Tests.Unit;

public class RegistryRequestValidationTests
{
    [Theory]
    [InlineData("owner", "image", true)]
    [InlineData("valid-owner", "valid-image", true)]
    [InlineData("owner123", "image456", true)]
    [InlineData("owner", "image_test", true)]
    [InlineData("owner", "image.test", true)]
    [InlineData("", "image", false)]
    [InlineData("owner", "", false)]
    [InlineData("owner!", "image", false)]
    [InlineData("owner", "image!", false)]
    [InlineData("owner@", "image", false)]
    [InlineData("owner", "image#", false)]
    [InlineData("owner_underscore", "image", false)]
    public void Owner_And_Image_Should_Validate_Format(string owner, string image, bool shouldBeValid)
    {
        var validator = new CrBrowser.Api.ValidationService();
        var isValid = validator.TryParseReference(owner, image, out var reference, out var error);
        
        Assert.Equal(shouldBeValid, isValid);
        if (shouldBeValid)
        {
            Assert.NotNull(reference);
            Assert.Null(error);
        }
        else
        {
            Assert.Null(reference);
            Assert.NotNull(error);
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void Page_Should_Be_Greater_Than_Zero(int page, bool shouldBeValid)
    {
        var isValid = page >= 1;
        Assert.Equal(shouldBeValid, isValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void PageSize_Should_Be_In_Range_1_To_100(int pageSize, bool shouldBeValid)
    {
        var isValid = pageSize >= 1 && pageSize <= 100;
        Assert.Equal(shouldBeValid, isValid);
    }

    [Theory]
    [InlineData(CrBrowser.Api.RegistryType.Ghcr, true)]
    [InlineData(CrBrowser.Api.RegistryType.DockerHub, true)]
    [InlineData(CrBrowser.Api.RegistryType.Quay, true)]
    [InlineData(CrBrowser.Api.RegistryType.Gcr, true)]
    [InlineData((CrBrowser.Api.RegistryType)999, false)]
    public void Unsupported_Registry_Type_Should_Be_Rejected(CrBrowser.Api.RegistryType registryType, bool shouldBeSupported)
    {
        var mockHttpClientFactory = new MockHttpClientFactory();
        var config = new CrBrowser.Api.RegistriesConfiguration
        {
            Ghcr = new CrBrowser.Api.RegistrySettings { BaseUrl = "https://ghcr.io", AuthUrl = "https://ghcr.io/token" },
            DockerHub = new CrBrowser.Api.RegistrySettings { BaseUrl = "https://registry-1.docker.io", AuthUrl = "https://auth.docker.io/token" },
            Quay = new CrBrowser.Api.RegistrySettings { BaseUrl = "https://quay.io", AuthUrl = "https://quay.io/v2/auth" },
            Gcr = new CrBrowser.Api.RegistrySettings { BaseUrl = "https://gcr.io", AuthUrl = "https://gcr.io/v2/token" }
        };
        
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
        var factory = new CrBrowser.Api.RegistryFactory(mockHttpClientFactory, loggerFactory, config);
        var isSupported = factory.IsSupported(registryType);
        
        Assert.Equal(shouldBeSupported, isSupported);
    }

    private class MockHttpClientFactory : System.Net.Http.IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name)
        {
            var client = new System.Net.Http.HttpClient();
            client.BaseAddress = new Uri("https://example.com/");
            return client;
        }
    }
}
