using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CrBrowser.Api;

public sealed class CustomOciRegistryClient : OciRegistryClientBase
{
    private readonly string _baseUrl;
    
    public override RegistryType RegistryType => RegistryType.Custom;
    public override string BaseUrl => _baseUrl;

    public CustomOciRegistryClient(string baseUrl, HttpClient http, ILogger<CustomOciRegistryClient> logger)
        : base(http, logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public override string FormatFullReference(string owner, string image, string tag)
    {
        var host = new Uri(_baseUrl).Host;
        return $"{host}/{owner}/{image}:{tag}";
    }

    public override async Task<BrowseImagesResponse> ListImagesAsync(
        string owner, 
        int pageSize, 
        string? authToken = null, 
        string? nextPageUrl = null, 
        CancellationToken ct = default)
    {
        try
        {
            var catalogUrl = nextPageUrl ?? $"{_baseUrl}/v2/_catalog?n={pageSize}";
            var req = new HttpRequestMessage(HttpMethod.Get, catalogUrl);
            
            if (authToken != null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            var resp = await _http.SendAsync(req, ct);

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Catalog endpoint /v2/_catalog not supported by registry");
                throw new CatalogNotSupportedException($"Registry at {_baseUrl} does not support the OCI catalog API. Direct image access is required.");
            }

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Catalog request failed with status {StatusCode}", (int)resp.StatusCode);
                return new BrowseImagesResponse(
                    Array.Empty<ImageListing>(),
                    null,
                    null
                );
            }

            var catalogData = await resp.Content.ReadFromJsonAsync<CatalogResponse>(ct);
            if (catalogData?.Repositories == null)
            {
                return new BrowseImagesResponse(Array.Empty<ImageListing>(), null, null);
            }

            var images = catalogData.Repositories
                .Where(repo => repo.StartsWith($"{owner}/"))
                .Select(repo =>
                {
                    var imageName = repo.Substring(owner.Length + 1);
                    return new ImageListing(
                        owner,
                        imageName,
                        RegistryType.Custom,
                        null,
                        null,
                        new ImageMetadata()
                    );
                })
                .ToArray();

            string? nextUrl = null;
            if (resp.Headers.TryGetValues("Link", out var linkHeaders))
            {
                var linkHeader = linkHeaders.FirstOrDefault();
                if (linkHeader != null)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(linkHeader, @"<([^>]+)>;\s*rel=""next""");
                    if (match.Success)
                    {
                        nextUrl = match.Groups[1].Value;
                    }
                }
            }

            return new BrowseImagesResponse(images, null, nextUrl);
        }
        catch (CatalogNotSupportedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing images from custom registry catalog");
            return new BrowseImagesResponse(Array.Empty<ImageListing>(), null, null);
        }
    }

    private sealed record CatalogResponse(string[]? Repositories = null);

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"v2/{repository}/tags/list?n=1");
        var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!resp.Headers.WwwAuthenticate.Any())
        {
            _logger.LogWarning("401 but no WWW-Authenticate header for custom registry");
            return null;
        }

        var authHeader = resp.Headers.WwwAuthenticate.FirstOrDefault();
        if (authHeader?.Scheme != "Bearer" || authHeader.Parameter == null)
        {
            _logger.LogWarning("Unexpected auth scheme: {Scheme}", authHeader?.Scheme);
            return null;
        }

        var parts = authHeader.Parameter
            .Split(',')
            .Select(p => p.Trim())
            .Select(p => p.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .ToDictionary(kv => kv[0], kv => kv[1].Trim('"'));

        if (!parts.TryGetValue("realm", out var realm))
        {
            _logger.LogWarning("No realm in WWW-Authenticate header");
            return null;
        }

        var tokenUrl = realm;
        if (parts.TryGetValue("service", out var service))
        {
            tokenUrl += $"?service={Uri.EscapeDataString(service)}";
        }
        if (parts.TryGetValue("scope", out var scope))
        {
            tokenUrl += $"&scope={Uri.EscapeDataString(scope)}";
        }

        _logger.LogInformation("Acquiring token from {TokenUrl}", tokenUrl);

        try
        {
            using var tokenClient = new HttpClient();
            using var tokenReq = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
            using var tokenResp = await tokenClient.SendAsync(tokenReq, ct);

            if (!tokenResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token request failed with status {StatusCode}", (int)tokenResp.StatusCode);
                return null;
            }

            var tokenData = await tokenResp.Content.ReadFromJsonAsync<TokenResponse>(ct);
            return tokenData?.Token ?? tokenData?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire token from {TokenUrl}", tokenUrl);
            return null;
        }
    }

    private sealed record TokenResponse(string? Token = null, string? AccessToken = null);
}
