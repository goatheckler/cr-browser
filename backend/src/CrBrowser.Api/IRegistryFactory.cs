namespace CrBrowser.Api;

public interface IRegistryFactory
{
    IContainerRegistryClient CreateClient(RegistryType registryType);
    IEnumerable<RegistryType> GetSupportedRegistries();
    bool IsSupported(RegistryType registryType);
}
