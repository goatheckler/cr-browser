using System.Net.Http.Headers;
using System.Text.Json;

namespace CrBrowser.Api;

public abstract class OciRegistryClientBase : IContainerRegistryClient
{
    protected readonly HttpClient _http;
    protected static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public abstract RegistryType RegistryType { get; }
    public abstract string BaseUrl { get; }

    protected OciRegistryClientBase(HttpClient http)
    {
        _http = http;
    }

    protected abstract Task<string?> AcquireTokenAsync(string repository, CancellationToken ct);
    
    protected virtual string FormatRepositoryPath(string owner, string image)
    {
        return $"{owner}/{image}".ToLowerInvariant();
    }

    public abstract string FormatFullReference(string owner, string image, string tag);

    private sealed record TagListResponse(List<string> Tags, string? Name = null);

    public async Task<RegistryResponse> ListTagsPageAsync(
        string owner, 
        string image, 
        int pageSize, 
        string? last, 
        CancellationToken ct = default)
    {
        var repository = FormatRepositoryPath(owner, image);
        var url = $"v2/{repository}/tags/list?n={pageSize}" + 
                  (string.IsNullOrEmpty(last) ? string.Empty : $"&last={Uri.EscapeDataString(last)}");

        var (resp, retry, notFound) = await SendAsync(url, bearer: null, ct);
        if (notFound) return new RegistryResponse(Array.Empty<string>(), true, false, false);
        
        if (resp is null && retry)
        {
            var token = await AcquireTokenAsync(repository, ct);
            (resp, retry, notFound) = await SendAsync(url, token, ct);
            if (notFound) return new RegistryResponse(Array.Empty<string>(), true, false, false);
            
            if (resp is null)
            {
                return new RegistryResponse(Array.Empty<string>(), false, retry, false);
            }
        }

        if (resp is null)
        {
            return new RegistryResponse(Array.Empty<string>(), false, retry, false);
        }

        try
        {
            await using var s = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<TagListResponse>(s, JsonOpts, ct);
            var tags = data?.Tags ?? new List<string>();
            bool hasMore = false;

            if (resp.Headers.TryGetValues("Link", out var linkVals))
            {
                foreach (var link in linkVals)
                {
                    if (link.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
                    {
                        hasMore = true;
                        break;
                    }
                }
            }
            else
            {
                if (tags.Count == pageSize) hasMore = true;
            }

            return new RegistryResponse(tags, false, false, hasMore);
        }
        catch
        {
            return new RegistryResponse(Array.Empty<string>(), false, true, false);
        }
    }

    protected async Task<(HttpResponseMessage? Response, bool Retryable, bool NotFound)> SendAsync(
        string relativeUrl, 
        string? bearer, 
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        if (!string.IsNullOrWhiteSpace(bearer))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var resp = await _http.SendAsync(req, ct);
        
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, false, true);
        }
        if ((int)resp.StatusCode == 429 || (int)resp.StatusCode >= 500)
        {
            return (null, true, false);
        }
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return (null, true, false);
        }
        if (!resp.IsSuccessStatusCode)
        {
            return (null, false, false);
        }
        
        return (resp, false, false);
    }
}
