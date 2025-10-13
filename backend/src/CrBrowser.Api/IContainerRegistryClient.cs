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
    
    Task<BrowseImagesResponse> ListImagesAsync(
        string owner,
        int pageSize,
        string? authToken = null,
        string? nextPageUrl = null,
        CancellationToken ct = default);
    
    string FormatFullReference(string owner, string image, string tag);
}
