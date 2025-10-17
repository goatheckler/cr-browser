namespace CrBrowser.Api;

public interface IRegistryFactory
{
    IContainerRegistryClient CreateClient(RegistryType registryType);
    IContainerRegistryClient CreateCustomClient(string baseUrl);
    IEnumerable<RegistryType> GetSupportedRegistries();
    bool IsSupported(RegistryType registryType);
}
