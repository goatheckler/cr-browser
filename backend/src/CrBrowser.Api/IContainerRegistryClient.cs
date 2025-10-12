namespace CrBrowser.Api;

public interface IContainerRegistryClient
{
    RegistryType RegistryType { get; }
    
    string BaseUrl { get; }
    
    Task<RegistryResponse> ListTagsPageAsync(
        string owner, 
        string image, 
        int pageSize, 
        string? last, 
        CancellationToken ct = default);
    
    string FormatFullReference(string owner, string image, string tag);
}
