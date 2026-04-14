using RawV.Models;

namespace RawV.Services;

public interface IImageCatalogService
{
    BrowserSession CreateFromDirectory(string directoryPath);

    BrowserSession CreateFromFiles(IEnumerable<string> filePaths);
}
