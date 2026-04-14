using Avalonia.Media.Imaging;

namespace RawV.Services;

public interface IImageLoaderService
{
    Task<Bitmap> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
