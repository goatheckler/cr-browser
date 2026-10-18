using System.Text.Json;

namespace CrBrowser.Api;

public sealed class DockerHubClient : OciRegistryClientBase
{
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _repository;
    private string? _token;
    private DateTimeOffset _tokenExpiresAt;

    public override RegistryType RegistryType => RegistryType.DockerHub;
    public override string BaseUrl => "https://registry-1.docker.io";

    public DockerHubClient(HttpClient http, ILogger<DockerHubClient> logger) : base(http, logger)
    {
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://registry-1.docker.io/");
    }

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        // Docker Hub tokens are scoped to a single repository; cache per repository
        // (and reuse the shared HttpClient) so paginating tags doesn't re-fetch a
        // token for every page - each page previously cost 3 serial round trips.
        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_repository == repository
                && _token != null
                && DateTimeOffset.UtcNow < _tokenExpiresAt)
            {
                return _token;
            }

            var authUrl = $"https://auth.docker.io/token?service=registry.docker.io&scope=repository:{repository}:pull";
            
            using var req = new HttpRequestMessage(HttpMethod.Get, authUrl);
            var resp = await _http.SendAsync(req, ct);
            
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Docker Hub token request failed with status {StatusCode} for {Repository}", resp.StatusCode, repository);
                return null;
            }
            
            try
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
                if (doc.RootElement.TryGetProperty("token", out var t))
                {
                    _token = t.GetString();
                    _repository = repository;
                    _tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5); // default TTL when 'expires_in' absent
                    if (doc.RootElement.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var expiresIn) && expiresIn > 0)
                        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

                    return _token;
                }
                
                _logger.LogWarning("Docker Hub token response missing 'token' property for {Repository}", repository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Docker Hub token response for {Repository}", repository);
            }
            
            return null;
        }
        finally
        {
            _tokenLock.Release();
        }
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
