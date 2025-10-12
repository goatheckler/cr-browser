using System.Text.Json;

namespace CrBrowser.Api;

public sealed class GcrClient : IContainerRegistryClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    
    public RegistryType RegistryType => RegistryType.Gcr;
    public string BaseUrl => "https://gcr.io";

    private sealed record GcrTagListResponse(List<string>? Tags = null, string? Name = null);

    public GcrClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://gcr.io/");
    }

    public async Task<RegistryResponse> ListTagsPageAsync(
        string owner, 
        string image, 
        int pageSize, 
        string? last, 
        CancellationToken ct = default)
    {
        var repository = FormatRepositoryPath(owner, image);
        var url = $"v2/{repository}/tags/list";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var resp = await _http.SendAsync(req, ct);
        
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new RegistryResponse(Array.Empty<string>(), true, false, false);
        }
        if ((int)resp.StatusCode == 429 || (int)resp.StatusCode >= 500)
        {
            return new RegistryResponse(Array.Empty<string>(), false, true, false);
        }
        if (!resp.IsSuccessStatusCode)
        {
            return new RegistryResponse(Array.Empty<string>(), true, false, false);
        }

        try
        {
            await using var s = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<GcrTagListResponse>(s, JsonOpts, ct);
            var allTags = data?.Tags ?? new List<string>();
            
            if (allTags.Count == 0)
            {
                return new RegistryResponse(Array.Empty<string>(), true, false, false);
            }

            var skip = 0;
            if (!string.IsNullOrEmpty(last))
            {
                var lastIndex = allTags.IndexOf(last);
                if (lastIndex >= 0)
                {
                    skip = lastIndex + 1;
                }
            }

            var pageTags = allTags.Skip(skip).Take(pageSize).ToList();
            var hasMore = skip + pageTags.Count < allTags.Count;

            return new RegistryResponse(pageTags, false, false, hasMore);
        }
        catch
        {
            return new RegistryResponse(Array.Empty<string>(), false, true, false);
        }
    }

    private string FormatRepositoryPath(string owner, string image)
    {
        return $"{owner}/{image}".ToLowerInvariant();
    }

    public string FormatFullReference(string owner, string image, string tag)
    {
        var repo = FormatRepositoryPath(owner, image);
        return $"gcr.io/{repo}:{tag}";
    }
}
