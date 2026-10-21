using WebsiteAPI.Contracts;
using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Data;
using WebsiteAPI.Entities;

namespace WebsiteAPI.Services;

public sealed class UploadService(IWebHostEnvironment environment, ApplicationDbContext context)
{
    public const long MaximumFileSize = 8 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif"
        };

    private readonly string uploadsPath = Path.Combine(environment.ContentRootPath, "Uploads");

    public async Task<IReadOnlyList<UploadResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(uploadsPath);
        var files = new DirectoryInfo(uploadsPath)
            .EnumerateFiles()
            .Where(file => AllowedTypes.TryGetValue(file.Extension, out _))
            .ToList();
        var assets = await context.MediaAssets
            .Where(asset => asset.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var knownKeys = assets.Select(asset => asset.StorageKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Where(file => !knownKeys.Contains(file.Name)))
        {
            var asset = new MediaAsset
            {
                Id = Guid.NewGuid(),
                FileName = file.Name,
                StorageKey = file.Name,
                Url = $"/uploads/{Uri.EscapeDataString(file.Name)}",
                MimeType = AllowedTypes[file.Extension],
                FileSize = file.Length,
                CreatedAt = file.CreationTimeUtc
            };
            context.MediaAssets.Add(asset);
            assets.Add(asset);
        }
        if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync(cancellationToken);
        return assets
            .Where(asset => files.Any(file => string.Equals(file.Name, asset.StorageKey, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(asset => asset.CreatedAt)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<UploadResponse> UploadAsync(
        IFormFile image,
        CancellationToken cancellationToken,
        int? uploadedByUserId = null)
    {
        if (image.Length is <= 0 or > MaximumFileSize)
        {
            throw new InvalidDataException("The image must be between 1 byte and 8 MB.");
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(extension, out var expectedContentType) ||
            !string.Equals(image.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only JPG, PNG, WebP, and GIF images are allowed.");
        }

        if (!await HasValidSignatureAsync(image, extension, cancellationToken))
        {
            throw new InvalidDataException("The uploaded file is not a valid image.");
        }

        Directory.CreateDirectory(uploadsPath);
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(uploadsPath, fileName);

        await using (var stream = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous))
        {
            await image.CopyToAsync(stream, cancellationToken);
        }

        var file = new FileInfo(destination);
        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            FileName = file.Name,
            StorageKey = file.Name,
            Url = $"/uploads/{Uri.EscapeDataString(file.Name)}",
            MimeType = expectedContentType,
            FileSize = file.Length,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = file.CreationTimeUtc
        };
        context.MediaAssets.Add(asset);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(asset);
    }

    public async Task<bool> DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) ||
            !AllowedTypes.ContainsKey(Path.GetExtension(fileName)))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(Path.Combine(uploadsPath, fileName));
        var rootPath = Path.GetFullPath(uploadsPath) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        var asset = await context.MediaAssets.SingleOrDefaultAsync(
            candidate => candidate.StorageKey == fileName && candidate.DeletedAt == null,
            cancellationToken);
        if (asset is not null)
        {
            asset.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    private static UploadResponse ToResponse(MediaAsset asset) => new(
        asset.Id,
        asset.FileName,
        asset.Url,
        asset.MimeType,
        asset.FileSize,
        asset.CreatedAt,
        asset.AltText,
        asset.Title);

    private static async Task<bool> HasValidSignatureAsync(
        IFormFile image,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = image.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        return extension switch
        {
            ".jpg" or ".jpeg" =>
                bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 && header[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".gif" => bytesRead >= 6 &&
                (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)),
            ".webp" => bytesRead >= 12 &&
                header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
