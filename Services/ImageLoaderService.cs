using Avalonia.Media.Imaging;

namespace RawV.Services;

public sealed class ImageLoaderService : IImageLoaderService
{
    public Task<Bitmap> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new Bitmap(filePath));
    }
}
