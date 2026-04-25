using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RawV.Services;
using RawV.ViewModels;
using RawV.Views;

namespace RawV;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var imageCatalogService = new ImageCatalogService();
            var imageLoaderService = new ImageLoaderService();
            var fileDeletionService = new FileDeletionService();
            var thumbnailService = new ThumbnailService();
            var mainWindowViewModel = new MainWindowViewModel(imageCatalogService, imageLoaderService, fileDeletionService, thumbnailService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
