using System.Text.Json;

namespace CrBrowser.Api;

public sealed class QuayClient : OciRegistryClientBase
{
    public override RegistryType RegistryType => RegistryType.Quay;
    public override string BaseUrl => "https://quay.io";

    public QuayClient(HttpClient http, ILogger<QuayClient> logger) : base(http, logger)
    {
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://quay.io/");
    }

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var authUrl = $"https://quay.io/v2/auth?service=quay.io&scope=repository:{repository}:pull";
        
        using var authClient = new HttpClient();
        authClient.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
        
        using var req = new HttpRequestMessage(HttpMethod.Get, authUrl);
        var resp = await authClient.SendAsync(req, ct);
        
        if (!resp.IsSuccessStatusCode) return null;
        
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("token", out var t))
                return t.GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Quay authentication token for repository {Repository}", repository);
        }
        
        return null;
    }

    public override string FormatFullReference(string owner, string image, string tag)
    {
        var repo = FormatRepositoryPath(owner, image);
        return $"quay.io/{repo}:{tag}";
    }

    public async override Task<BrowseImagesResponse> ListImagesAsync(
        string owner,
        int pageSize,
        string? authToken = null,
        string? nextPageUrl = null,
        CancellationToken ct = default)
    {
        var url = $"https://quay.io/api/v1/repository?namespace={owner}&public=true";

        using var quayClient = new HttpClient();
        quayClient.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
        
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var resp = await quayClient.SendAsync(req, ct);
        
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Quay API request failed with status {StatusCode} for namespace {Owner}", resp.StatusCode, owner);
            return new BrowseImagesResponse(Array.Empty<ImageListing>(), null, null);
        }

        var content = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        
        var images = new List<ImageListing>();
        if (doc.RootElement.TryGetProperty("repositories", out var repos))
        {
            foreach (var repo in repos.EnumerateArray())
            {
                var name = repo.GetProperty("name").GetString() ?? "";
                var ns = repo.TryGetProperty("namespace", out var nsProp) 
                    ? nsProp.GetString() ?? owner 
                    : owner;
                
                var lastUpdated = repo.TryGetProperty("last_modified", out var modified) 
                    ? DateTimeOffset.FromUnixTimeSeconds(modified.GetInt64()).DateTime 
                    : (DateTime?)null;
                
                var description = repo.TryGetProperty("description", out var desc) ? desc.GetString() : null;
                var isPublic = repo.TryGetProperty("is_public", out var pub) ? pub.GetBoolean() : (bool?)null;
                var state = repo.TryGetProperty("state", out var st) ? st.GetString() : null;

                images.Add(new ImageListing(
                    ns,
                    name,
                    RegistryType.Quay,
                    lastUpdated,
                    null,
                    new ImageMetadata(
                        Description: description,
                        IsPublic: isPublic,
                        RepositoryState: state
                    )
                ));
            }
        }

        return new BrowseImagesResponse(images.ToArray(), images.Count, null);
    }
}
