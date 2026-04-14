using RawV.Models;

namespace RawV.Services;

public interface IFileDeletionService
{
    Task<DeleteResult> DeleteToRecycleBinAsync(ImageEntry currentImage, IReadOnlyList<ImageEntry> allImages, CancellationToken cancellationToken = default);
}
