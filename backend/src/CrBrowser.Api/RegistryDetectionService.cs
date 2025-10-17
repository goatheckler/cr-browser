using System.Net;

namespace CrBrowser.Api;

public interface IRegistryDetectionService
{
    bool ValidateAndNormalizeUrl(string url, out string? normalizedUrl, out string? error);
    Task<RegistryDetectionResult> DetectRegistryAsync(string baseUrl, CancellationToken ct = default);
}

public record RegistryDetectionResult(
    bool Supported,
    string? NormalizedUrl,
    string? ApiVersion,
    RegistryCapabilities? Capabilities,
    string? ErrorMessage
);

public record RegistryCapabilities(
    bool Catalog,
    bool TagsList
);

public sealed class RegistryDetectionService : IRegistryDetectionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RegistryDetectionService> _logger;

    public RegistryDetectionService(IHttpClientFactory httpClientFactory, ILogger<RegistryDetectionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool ValidateAndNormalizeUrl(string url, out string? normalizedUrl, out string? error)
    {
        normalizedUrl = null;
        error = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL cannot be empty";
            return false;
        }

        url = url.Trim();

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = $"https://{url}";
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            error = "Invalid URL format";
            return false;
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            error = "URL must use HTTP or HTTPS";
            return false;
        }

        normalizedUrl = uri.ToString().TrimEnd('/');
        return true;
    }

    public async Task<RegistryDetectionResult> DetectRegistryAsync(string baseUrl, CancellationToken ct = default)
    {
        if (!ValidateAndNormalizeUrl(baseUrl, out var normalizedUrl, out var validationError))
        {
            return new RegistryDetectionResult(false, null, null, null, validationError);
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(normalizedUrl!);
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v2/");
            using var response = await httpClient.SendAsync(request, ct);

            _logger.LogInformation("Registry detection probe {Url} returned {StatusCode}", 
                normalizedUrl, (int)response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new RegistryDetectionResult(
                    false, 
                    normalizedUrl, 
                    null, 
                    null, 
                    "Registry does not support OCI Distribution API v2 (/v2/ endpoint not found)");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.IsSuccessStatusCode)
            {
                if (response.Headers.TryGetValues("Docker-Distribution-Api-Version", out var apiVersionValues))
                {
                    var apiVersion = apiVersionValues.FirstOrDefault();
                    _logger.LogInformation("Detected OCI registry at {Url} with API version {Version}", 
                        normalizedUrl, apiVersion);

                    var capabilities = new RegistryCapabilities(
                        Catalog: false,
                        TagsList: true
                    );

                    return new RegistryDetectionResult(
                        true, 
                        normalizedUrl, 
                        apiVersion, 
                        capabilities, 
                        null);
                }

                return new RegistryDetectionResult(
                    false, 
                    normalizedUrl, 
                    null, 
                    null, 
                    "Registry responded but did not return Docker-Distribution-Api-Version header");
            }

            return new RegistryDetectionResult(
                false, 
                normalizedUrl, 
                null, 
                null, 
                $"Registry returned unexpected status code: {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to registry {Url}", normalizedUrl);
            return new RegistryDetectionResult(
                false, 
                normalizedUrl, 
                null, 
                null, 
                $"Unable to connect to registry: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Registry detection timeout for {Url}", normalizedUrl);
            return new RegistryDetectionResult(
                false, 
                normalizedUrl, 
                null, 
                null, 
                "Connection timeout. Registry may be unreachable.");
        }
        catch (TaskCanceledException)
        {
            return new RegistryDetectionResult(
                false, 
                normalizedUrl, 
                null, 
                null, 
                "Request was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registry detection for {Url}", normalizedUrl);
            return new RegistryDetectionResult(
                false, 
                normalizedUrl, 
                null, 
                null, 
                $"Unexpected error: {ex.Message}");
        }
    }
}
