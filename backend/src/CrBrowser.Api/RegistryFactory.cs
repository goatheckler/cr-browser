namespace CrBrowser.Api;

public sealed class RegistryFactory : IRegistryFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HashSet<RegistryType> _supportedRegistries;
    private readonly RegistriesConfiguration _registriesConfig;

    public RegistryFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, RegistriesConfiguration registriesConfig)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _registriesConfig = registriesConfig ?? throw new ArgumentNullException(nameof(registriesConfig));
        
        _supportedRegistries = new HashSet<RegistryType>
        {
            RegistryType.Ghcr,
            RegistryType.DockerHub,
            RegistryType.Quay,
            RegistryType.Gcr
        };
    }

    public IContainerRegistryClient CreateClient(RegistryType registryType)
    {
        if (!IsSupported(registryType))
        {
            throw new NotSupportedException($"Registry type '{registryType}' is not supported");
        }

        return registryType switch
        {
            RegistryType.Ghcr => new GhcrClient(
                _httpClientFactory.CreateClient("GhcrClient"), 
                _loggerFactory.CreateLogger<GhcrClient>()),
            RegistryType.DockerHub => new DockerHubClient(
                _httpClientFactory.CreateClient("DockerHubClient"), 
                _loggerFactory.CreateLogger<DockerHubClient>()),
            RegistryType.Quay => new QuayClient(
                _httpClientFactory.CreateClient("QuayClient"), 
                _loggerFactory.CreateLogger<QuayClient>()),
            RegistryType.Gcr => new GcrClient(_httpClientFactory.CreateClient("GcrClient")),
            _ => throw new NotSupportedException($"Registry type '{registryType}' is not supported")
        };
    }

    public IEnumerable<RegistryType> GetSupportedRegistries()
    {
        return _supportedRegistries.AsEnumerable();
    }

    public bool IsSupported(RegistryType registryType)
    {
        return _supportedRegistries.Contains(registryType);
    }
}
