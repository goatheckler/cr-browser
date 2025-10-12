using System.Net.Http.Headers;
using System.Text.Json;

namespace CrBrowser.Api;

public abstract class OciRegistryClientBase : IContainerRegistryClient
{
    protected readonly HttpClient _http;
    protected readonly ILogger _logger;
    protected static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public abstract RegistryType RegistryType { get; }
    public abstract string BaseUrl { get; }

    protected OciRegistryClientBase(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
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

        _logger.LogInformation("Fetching tags for {Repository} from {Registry}", repository, RegistryType);
        
        var (resp, retry, notFound) = await SendAsync(url, bearer: null, ct);
        if (notFound)
        {
            _logger.LogInformation("Repository {Repository} not found (initial request)", repository);
            return new RegistryResponse(Array.Empty<string>(), true, false, false);
        }
        
        if (resp is null && retry)
        {
            _logger.LogInformation("Acquiring token for {Repository}", repository);
            var token = await AcquireTokenAsync(repository, ct);
            if (token == null)
            {
                _logger.LogWarning("Failed to acquire token for {Repository}", repository);
            }
            
            (resp, retry, notFound) = await SendAsync(url, token, ct);
            if (notFound)
            {
                _logger.LogInformation("Repository {Repository} not found (after token)", repository);
                return new RegistryResponse(Array.Empty<string>(), true, false, false);
            }
            
            // If we still get 401 after acquiring token, the repository likely doesn't exist
            if (resp is null && retry)
            {
                _logger.LogInformation("Repository {Repository} not found (401 after token acquisition)", repository);
                return new RegistryResponse(Array.Empty<string>(), true, false, false);
            }
            
            if (resp is null)
            {
                _logger.LogWarning("Request failed for {Repository} - Retry: {Retry}", repository, retry);
                return new RegistryResponse(Array.Empty<string>(), false, retry, false);
            }
        }

        if (resp is null)
        {
            _logger.LogWarning("No response for {Repository} - Retry: {Retry}", repository, retry);
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

            _logger.LogInformation("Retrieved {Count} tags for {Repository}", tags.Count, repository);
            return new RegistryResponse(tags, false, false, hasMore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse response for {Repository}", repository);
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
        
        _logger.LogInformation("HTTP {Method} {Url} -> {StatusCode}", req.Method, relativeUrl, (int)resp.StatusCode);
        
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, false, true);
        }
        // Treat 403 Forbidden as NotFound for public registries - typically means repo doesn't exist
        if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogInformation("Treating 403 Forbidden as NotFound for {Url}", relativeUrl);
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
            _logger.LogWarning("Unexpected status code {StatusCode} for {Url}", (int)resp.StatusCode, relativeUrl);
            return (null, false, false);
        }
        
        return (resp, false, false);
    }
}
