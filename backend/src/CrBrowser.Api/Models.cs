using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace CrBrowser.Api;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegistryType
{
    Ghcr,
    DockerHub,
    Quay,
    Gcr
}

public record RegistryConfiguration(
    RegistryType Type,
    string BaseUrl,
    string? AuthUrl = null
);

public class RegistriesConfiguration
{
    public RegistrySettings Ghcr { get; set; } = new();
    public RegistrySettings DockerHub { get; set; } = new();
    public RegistrySettings Quay { get; set; } = new();
    public RegistrySettings Gcr { get; set; } = new();
}

public class RegistrySettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthUrl { get; set; } = string.Empty;
}

public record RegistryResponse(
    IReadOnlyList<string> Tags,
    bool NotFound,
    bool Retryable,
    bool HasMore
);

public record ImageReference(string Owner, string Image)
{
    public override string ToString() => $"{Owner}/{Image}";
}

public interface IValidationService
{
    bool TryParseReference(string owner, string image, out ImageReference? reference, out string? error);
}

public sealed class ValidationService : IValidationService
{
    private static readonly Regex OwnerRegex = new("^[a-z0-9](?:[a-z0-9-]{0,38})$", RegexOptions.Compiled); // simplistic GitHub-like constraint
    private static readonly Regex ImageRegex = new("^[a-z0-9]+(?:[._-][a-z0-9]+)*$", RegexOptions.Compiled); // OCI-ish

    public bool TryParseReference(string owner, string image, out ImageReference? reference, out string? error)
    {
        reference = null;
        error = null;
        if (string.IsNullOrWhiteSpace(owner) || !OwnerRegex.IsMatch(owner))
        {
            error = "Invalid owner";
            return false;
        }
        if (string.IsNullOrWhiteSpace(image) || !ImageRegex.IsMatch(image))
        {
            error = "Invalid image";
            return false;
        }
        reference = new ImageReference(owner, image);
        return true;
    }
}

public record ErrorResponse(string Code, string Message, bool Retryable);

public record RegistryRequest(
    [Required] RegistryType RegistryType,
    [Required] [RegularExpression(@"^[a-z0-9](?:[a-z0-9-]{0,38})$")] string Owner,
    [Required] [RegularExpression(@"^[a-z0-9]+(?:[._-][a-z0-9]+)*$")] string Image,
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int PageSize = 10
);

public record ImageListing(
    string Owner,
    string ImageName,
    RegistryType RegistryType,
    DateTime? LastUpdated,
    DateTime? CreatedAt,
    ImageMetadata Metadata
);

public record ImageMetadata(
    string? Description = null,
    long? StarCount = null,
    long? PullCount = null,
    bool? IsPublic = null,
    string? RepositoryState = null,
    long? PackageId = null,
    string? Visibility = null,
    string? HtmlUrl = null,
    string? ProjectId = null
);

public record BrowseImagesResponse(
    ImageListing[] Images,
    int? TotalCount,
    string? NextPageUrl
);
