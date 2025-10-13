namespace CrBrowser.Tests.Unit;

public class RegistryFactoryTests
{
    private class MockRegistryFactory : CrBrowser.Api.IRegistryFactory
    {
        private readonly Dictionary<CrBrowser.Api.RegistryType, CrBrowser.Api.IContainerRegistryClient> _clients = new();
        private readonly HashSet<CrBrowser.Api.RegistryType> _supportedTypes = new();

        public MockRegistryFactory()
        {
            _supportedTypes.Add(CrBrowser.Api.RegistryType.Ghcr);
            _supportedTypes.Add(CrBrowser.Api.RegistryType.DockerHub);
            _supportedTypes.Add(CrBrowser.Api.RegistryType.Quay);
            _supportedTypes.Add(CrBrowser.Api.RegistryType.Gcr);
            
            _clients[CrBrowser.Api.RegistryType.Ghcr] = new MockGhcrClient();
            _clients[CrBrowser.Api.RegistryType.DockerHub] = new MockDockerHubClient();
            _clients[CrBrowser.Api.RegistryType.Quay] = new MockQuayClient();
            _clients[CrBrowser.Api.RegistryType.Gcr] = new MockGcrClient();
        }

        public CrBrowser.Api.IContainerRegistryClient CreateClient(CrBrowser.Api.RegistryType registryType)
        {
            if (!_supportedTypes.Contains(registryType))
            {
                throw new NotSupportedException($"Registry type '{registryType}' is not supported");
            }
            
            return _clients[registryType];
        }

        public IEnumerable<CrBrowser.Api.RegistryType> GetSupportedRegistries()
        {
            return _supportedTypes.AsEnumerable();
        }

        public bool IsSupported(CrBrowser.Api.RegistryType registryType)
        {
            return _supportedTypes.Contains(registryType);
        }
    }

    private class MockGhcrClient : CrBrowser.Api.IContainerRegistryClient
    {
        public CrBrowser.Api.RegistryType RegistryType => CrBrowser.Api.RegistryType.Ghcr;
        public string BaseUrl => "https://ghcr.io";

        public Task<CrBrowser.Api.RegistryResponse> ListTagsPageAsync(
            string owner, 
            string image, 
            int pageSize, 
            string? last, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.RegistryResponse(
                new List<string> { "v1.0.0" },
                NotFound: false,
                Retryable: false,
                HasMore: false
            ));
        }

        public Task<CrBrowser.Api.BrowseImagesResponse> ListImagesAsync(
            string owner, 
            int pageSize, 
            string? authToken = null, 
            string? nextPageUrl = null, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.BrowseImagesResponse(
                Array.Empty<CrBrowser.Api.ImageListing>(), 
                null, 
                null
            ));
        }

        public string FormatFullReference(string owner, string image, string tag)
        {
            return $"{BaseUrl}/{owner}/{image}:{tag}";
        }
    }

    private class MockDockerHubClient : CrBrowser.Api.IContainerRegistryClient
    {
        public CrBrowser.Api.RegistryType RegistryType => CrBrowser.Api.RegistryType.DockerHub;
        public string BaseUrl => "https://hub.docker.com";

        public Task<CrBrowser.Api.RegistryResponse> ListTagsPageAsync(
            string owner, 
            string image, 
            int pageSize, 
            string? last, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.RegistryResponse(
                new List<string> { "latest" },
                NotFound: false,
                Retryable: false,
                HasMore: false
            ));
        }

        public Task<CrBrowser.Api.BrowseImagesResponse> ListImagesAsync(
            string owner, 
            int pageSize, 
            string? authToken = null, 
            string? nextPageUrl = null, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.BrowseImagesResponse(
                Array.Empty<CrBrowser.Api.ImageListing>(), 
                null, 
                null
            ));
        }

        public string FormatFullReference(string owner, string image, string tag)
        {
            return $"{BaseUrl}/r/{owner}/{image}:{tag}";
        }
    }

    private class MockQuayClient : CrBrowser.Api.IContainerRegistryClient
    {
        public CrBrowser.Api.RegistryType RegistryType => CrBrowser.Api.RegistryType.Quay;
        public string BaseUrl => "https://quay.io";

        public Task<CrBrowser.Api.RegistryResponse> ListTagsPageAsync(
            string owner, 
            string image, 
            int pageSize, 
            string? last, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.RegistryResponse(
                new List<string> { "latest" },
                NotFound: false,
                Retryable: false,
                HasMore: false
            ));
        }

        public Task<CrBrowser.Api.BrowseImagesResponse> ListImagesAsync(
            string owner, 
            int pageSize, 
            string? authToken = null, 
            string? nextPageUrl = null, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.BrowseImagesResponse(
                Array.Empty<CrBrowser.Api.ImageListing>(), 
                null, 
                null
            ));
        }

        public string FormatFullReference(string owner, string image, string tag)
        {
            return $"{BaseUrl}/{owner}/{image}:{tag}";
        }
    }

    private class MockGcrClient : CrBrowser.Api.IContainerRegistryClient
    {
        public CrBrowser.Api.RegistryType RegistryType => CrBrowser.Api.RegistryType.Gcr;
        public string BaseUrl => "https://gcr.io";

        public Task<CrBrowser.Api.RegistryResponse> ListTagsPageAsync(
            string owner, 
            string image, 
            int pageSize, 
            string? last, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.RegistryResponse(
                new List<string> { "latest" },
                NotFound: false,
                Retryable: false,
                HasMore: false
            ));
        }

        public Task<CrBrowser.Api.BrowseImagesResponse> ListImagesAsync(
            string owner, 
            int pageSize, 
            string? authToken = null, 
            string? nextPageUrl = null, 
            CancellationToken ct = default)
        {
            return Task.FromResult(new CrBrowser.Api.BrowseImagesResponse(
                Array.Empty<CrBrowser.Api.ImageListing>(), 
                null, 
                null
            ));
        }

        public string FormatFullReference(string owner, string image, string tag)
        {
            return $"{BaseUrl}/{owner}/{image}:{tag}";
        }
    }

    [Fact]
    public void IRegistryFactory_Should_Have_CreateClient_Method()
    {
        var factory = new MockRegistryFactory();
        
        var client = factory.CreateClient(CrBrowser.Api.RegistryType.Ghcr);
        
        Assert.NotNull(client);
        Assert.IsAssignableFrom<CrBrowser.Api.IContainerRegistryClient>(client);
    }

    [Fact]
    public void CreateClient_Should_Return_Correct_Client_For_Ghcr()
    {
        var factory = new MockRegistryFactory();
        
        var client = factory.CreateClient(CrBrowser.Api.RegistryType.Ghcr);
        
        Assert.Equal(CrBrowser.Api.RegistryType.Ghcr, client.RegistryType);
        Assert.Equal("https://ghcr.io", client.BaseUrl);
    }

    [Fact]
    public void CreateClient_Should_Return_Correct_Client_For_DockerHub()
    {
        var factory = new MockRegistryFactory();
        
        var client = factory.CreateClient(CrBrowser.Api.RegistryType.DockerHub);
        
        Assert.Equal(CrBrowser.Api.RegistryType.DockerHub, client.RegistryType);
        Assert.Equal("https://hub.docker.com", client.BaseUrl);
    }

    [Fact]
    public void CreateClient_Should_Throw_NotSupportedException_For_Unsupported_Registry()
    {
        var factory = new MockRegistryFactory();
        
        var exception = Assert.Throws<NotSupportedException>(() => 
            factory.CreateClient((CrBrowser.Api.RegistryType)999)
        );
        
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void IRegistryFactory_Should_Have_GetSupportedRegistries_Method()
    {
        var factory = new MockRegistryFactory();
        
        var supportedRegistries = factory.GetSupportedRegistries();
        
        Assert.NotNull(supportedRegistries);
    }

    [Fact]
    public void GetSupportedRegistries_Should_Return_All_Supported_Registries()
    {
        var factory = new MockRegistryFactory();
        
        var supportedRegistries = factory.GetSupportedRegistries().ToList();
        
        Assert.Contains(CrBrowser.Api.RegistryType.Ghcr, supportedRegistries);
        Assert.Contains(CrBrowser.Api.RegistryType.DockerHub, supportedRegistries);
        Assert.Contains(CrBrowser.Api.RegistryType.Quay, supportedRegistries);
        Assert.Contains(CrBrowser.Api.RegistryType.Gcr, supportedRegistries);
        Assert.Equal(4, supportedRegistries.Count);
    }

    [Fact]
    public void GetSupportedRegistries_Should_Not_Include_Unsupported_Registries()
    {
        var factory = new MockRegistryFactory();
        
        var supportedRegistries = factory.GetSupportedRegistries().ToList();
        
        Assert.DoesNotContain((CrBrowser.Api.RegistryType)999, supportedRegistries);
    }

    [Fact]
    public void IRegistryFactory_Should_Have_IsSupported_Method()
    {
        var factory = new MockRegistryFactory();
        
        var isSupported = factory.IsSupported(CrBrowser.Api.RegistryType.Ghcr);
        
        Assert.True(isSupported);
    }

    [Fact]
    public void IsSupported_Should_Return_True_For_Ghcr()
    {
        var factory = new MockRegistryFactory();
        
        var isSupported = factory.IsSupported(CrBrowser.Api.RegistryType.Ghcr);
        
        Assert.True(isSupported);
    }

    [Fact]
    public void IsSupported_Should_Return_True_For_DockerHub()
    {
        var factory = new MockRegistryFactory();
        
        var isSupported = factory.IsSupported(CrBrowser.Api.RegistryType.DockerHub);
        
        Assert.True(isSupported);
    }

    [Fact]
    public void IsSupported_Should_Return_True_For_Quay()
    {
        var factory = new MockRegistryFactory();
        
        var isSupported = factory.IsSupported(CrBrowser.Api.RegistryType.Quay);
        
        Assert.True(isSupported);
    }

    [Fact]
    public void IsSupported_Should_Return_True_For_Gcr()
    {
        var factory = new MockRegistryFactory();
        
        var isSupported = factory.IsSupported(CrBrowser.Api.RegistryType.Gcr);
        
        Assert.True(isSupported);
    }

    [Fact]
    public void IsSupported_Should_Return_False_For_Unsupported_Registry()
    {
        var factory = new MockRegistryFactory();
        
        var isSupported = factory.IsSupported((CrBrowser.Api.RegistryType)999);
        
        Assert.False(isSupported);
    }

    [Fact]
    public async Task Created_Client_Should_Be_Functional()
    {
        var factory = new MockRegistryFactory();
        
        var client = factory.CreateClient(CrBrowser.Api.RegistryType.Ghcr);
        var response = await client.ListTagsPageAsync("owner", "image", 100, null);
        
        Assert.NotNull(response);
        Assert.NotEmpty(response.Tags);
    }

    [Fact]
    public void Real_RegistryFactory_Should_Accept_Configuration()
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
        
        Assert.NotNull(factory);
        Assert.True(factory.IsSupported(CrBrowser.Api.RegistryType.Ghcr));
        Assert.True(factory.IsSupported(CrBrowser.Api.RegistryType.DockerHub));
        Assert.True(factory.IsSupported(CrBrowser.Api.RegistryType.Quay));
        Assert.True(factory.IsSupported(CrBrowser.Api.RegistryType.Gcr));
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
