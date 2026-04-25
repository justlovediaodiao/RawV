using Avalonia.Media.Imaging;

namespace RawV.Services;

public interface IThumbnailService
{
    Task<Bitmap?> GetThumbnailAsync(string filePath, int width, int height, CancellationToken cancellationToken = default);
    void InvalidateCache(string filePath);
    void ClearCache();
}
