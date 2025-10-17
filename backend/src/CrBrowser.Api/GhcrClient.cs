using System.Text.Json;

namespace CrBrowser.Api;

public interface IGhcrClient
{
    Task<(IReadOnlyList<string> Tags, bool NotFound, bool Retryable, bool HasMore)> ListTagsPageAsync(string owner, string image, int pageSize, string? last, CancellationToken ct = default);
}

public sealed class GhcrClient : OciRegistryClientBase, IGhcrClient
{
    public override RegistryType RegistryType => RegistryType.Ghcr;
    public override string BaseUrl => "https://ghcr.io";

    public GhcrClient(HttpClient http, ILogger<GhcrClient> logger) : base(http, logger)
    {
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://ghcr.io/");
    }

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var tokenUrl = $"token?scope=repository:{repository}:pull&service=ghcr.io";
        using var req = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Token request failed with status {StatusCode} for {Repository}", resp.StatusCode, repository);
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("token", out var t))
                return t.GetString();
            
            _logger.LogWarning("Token response missing 'token' property for {Repository}", repository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse token response for {Repository}", repository);
        }
        return null;
    }

    public override string FormatFullReference(string owner, string image, string tag)
    {
        var repo = FormatRepositoryPath(owner, image);
        return $"ghcr.io/{repo}:{tag}";
    }

    async Task<(IReadOnlyList<string> Tags, bool NotFound, bool Retryable, bool HasMore)> IGhcrClient.ListTagsPageAsync(
        string owner, 
        string image, 
        int pageSize, 
        string? last, 
        CancellationToken ct)
    {
        var response = await base.ListTagsPageAsync(owner, image, pageSize, last, ct);
        return (response.Tags, response.NotFound, response.Retryable, response.HasMore);
    }

    public async override Task<BrowseImagesResponse> ListImagesAsync(
        string owner,
        int pageSize,
        string? authToken = null,
        string? nextPageUrl = null,
        CancellationToken ct = default)
    {
        var ownerType = owner.Contains('-') ? "orgs" : "users";
        var url = nextPageUrl ?? $"https://api.github.com/{ownerType}/{owner}/packages?package_type=container&per_page={Math.Min(pageSize, 100)}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Accept", "application/vnd.github+json");
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        
        if (!string.IsNullOrEmpty(authToken))
        {
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        }

        var resp = await _http.SendAsync(req, ct);
        
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("GitHub API request failed with status {StatusCode} for owner {Owner}", resp.StatusCode, owner);
            return new BrowseImagesResponse(Array.Empty<ImageListing>(), null, null);
        }

        var content = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        
        var images = new List<ImageListing>();
        foreach (var pkg in doc.RootElement.EnumerateArray())
        {
            var name = pkg.GetProperty("name").GetString() ?? "";
            var ownerLogin = pkg.TryGetProperty("owner", out var ownerProp) && 
                           ownerProp.TryGetProperty("login", out var loginProp)
                ? loginProp.GetString() ?? owner
                : owner;
            
            var lastUpdated = pkg.TryGetProperty("updated_at", out var updated) 
                ? DateTime.Parse(updated.GetString()!) 
                : (DateTime?)null;
            var createdAt = pkg.TryGetProperty("created_at", out var created) 
                ? DateTime.Parse(created.GetString()!) 
                : (DateTime?)null;
            
            var packageId = pkg.TryGetProperty("id", out var id) ? id.GetInt64() : (long?)null;
            var visibility = pkg.TryGetProperty("visibility", out var vis) ? vis.GetString() : null;
            var htmlUrl = pkg.TryGetProperty("html_url", out var html) ? html.GetString() : null;

            images.Add(new ImageListing(
                ownerLogin,
                name,
                RegistryType.Ghcr,
                lastUpdated,
                createdAt,
                new ImageMetadata(
                    PackageId: packageId,
                    Visibility: visibility,
                    HtmlUrl: htmlUrl
                )
            ));
        }

        var linkHeader = resp.Headers.TryGetValues("Link", out var links) ? links.FirstOrDefault() : null;
        var next = ExtractNextPageUrl(linkHeader);

        return new BrowseImagesResponse(images.ToArray(), images.Count, next);
    }

    private static string? ExtractNextPageUrl(string? linkHeader)
    {
        if (string.IsNullOrEmpty(linkHeader)) return null;
        
        var parts = linkHeader.Split(',');
        foreach (var part in parts)
        {
            if (part.Contains("rel=\"next\""))
            {
                var match = System.Text.RegularExpressions.Regex.Match(part, "<(.+?)>");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        return null;
    }
}

