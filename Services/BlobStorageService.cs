using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };
    private const long MaxBytes = 5 * 1024 * 1024;
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration config)
    {
        var cs = config["AzureBlob:ConnectionString"]
                 ?? throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");
        _container = new BlobContainerClient(cs, config["AzureBlob:ContainerName"] ?? "raceday-images");
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!Allowed.TryGetValue(ext, out var contentType))
            throw new ArgumentException("Only JPG, PNG or WEBP images are allowed.");
        if (file.Length == 0 || file.Length > MaxBytes)
            throw new ArgumentException("The image must be smaller than 5 MB.");

        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        var blob = _container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blob.Uri.ToString();
    }
}