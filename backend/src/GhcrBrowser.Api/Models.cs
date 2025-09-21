using System.Text.RegularExpressions;

namespace GhcrBrowser.Api;

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

// Removed legacy Tag / TagPage / Truncation models in simplified tags-only API.
public record ErrorResponse(string Code, string Message, bool Retryable);
