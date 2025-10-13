using System.Text.Json;

namespace CrBrowser.Api;

public sealed class DockerHubClient : OciRegistryClientBase
{
    public override RegistryType RegistryType => RegistryType.DockerHub;
    public override string BaseUrl => "https://registry-1.docker.io";

    public DockerHubClient(HttpClient http, ILogger<DockerHubClient> logger) : base(http, logger)
    {
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://registry-1.docker.io/");
    }

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var authUrl = $"https://auth.docker.io/token?service=registry.docker.io&scope=repository:{repository}:pull";
        
        using var authClient = new HttpClient();
        authClient.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
        
        using var req = new HttpRequestMessage(HttpMethod.Get, authUrl);
        var resp = await authClient.SendAsync(req, ct);
        
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Docker Hub token request failed with status {StatusCode} for {Repository}", resp.StatusCode, repository);
            return null;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("token", out var t))
                return t.GetString();
            
            _logger.LogWarning("Docker Hub token response missing 'token' property for {Repository}", repository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Docker Hub token response for {Repository}", repository);
        }
        
        return null;
    }

    protected override string FormatRepositoryPath(string owner, string image)
    {
        if (string.IsNullOrEmpty(owner) || owner.Equals("library", StringComparison.OrdinalIgnoreCase))
        {
            return $"library/{image}".ToLowerInvariant();
        }
        
        return $"{owner}/{image}".ToLowerInvariant();
    }

    public override string FormatFullReference(string owner, string image, string tag)
    {
        var repo = FormatRepositoryPath(owner, image);
        return $"docker.io/{repo}:{tag}";
    }

    public async override Task<BrowseImagesResponse> ListImagesAsync(
        string owner,
        int pageSize,
        string? authToken = null,
        string? nextPageUrl = null,
        CancellationToken ct = default)
    {
        var url = nextPageUrl ?? $"https://hub.docker.com/v2/repositories/{owner}/?page_size={Math.Min(pageSize, 100)}";

        using var hubClient = new HttpClient();
        hubClient.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
        
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var resp = await hubClient.SendAsync(req, ct);
        
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Docker Hub API request failed with status {StatusCode} for namespace {Owner}", resp.StatusCode, owner);
            return new BrowseImagesResponse(Array.Empty<ImageListing>(), null, null);
        }

        var content = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        
        var images = new List<ImageListing>();
        if (doc.RootElement.TryGetProperty("results", out var results))
        {
            foreach (var repo in results.EnumerateArray())
            {
                var name = repo.GetProperty("name").GetString() ?? "";
                var ns = repo.TryGetProperty("namespace", out var nsProp) 
                    ? nsProp.GetString() ?? owner 
                    : owner;
                
                var lastUpdated = repo.TryGetProperty("last_updated", out var updated) 
                    ? DateTime.Parse(updated.GetString()!) 
                    : (DateTime?)null;
                
                var description = repo.TryGetProperty("description", out var desc) ? desc.GetString() : null;
                var starCount = repo.TryGetProperty("star_count", out var stars) ? stars.GetInt64() : (long?)null;
                var pullCount = repo.TryGetProperty("pull_count", out var pulls) ? pulls.GetInt64() : (long?)null;

                images.Add(new ImageListing(
                    ns,
                    name,
                    RegistryType.DockerHub,
                    lastUpdated,
                    null,
                    new ImageMetadata(
                        Description: description,
                        StarCount: starCount,
                        PullCount: pullCount
                    )
                ));
            }
        }

        var totalCount = doc.RootElement.TryGetProperty("count", out var count) ? count.GetInt32() : (int?)null;
        var next = doc.RootElement.TryGetProperty("next", out var nextProp) && nextProp.ValueKind != JsonValueKind.Null
            ? nextProp.GetString() 
            : null;

        return new BrowseImagesResponse(images.ToArray(), totalCount, next);
    }
}
