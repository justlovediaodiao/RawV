using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RawV.Models;
using RawV.Services;

namespace RawV.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IImageCatalogService _imageCatalogService;
    private readonly IImageLoaderService _imageLoaderService;
    private readonly IFileDeletionService _fileDeletionService;

    [ObservableProperty]
    private Bitmap? currentBitmap;

    [ObservableProperty]
    private BrowserSession currentSession = new(Array.Empty<ImageEntry>(), -1);

    [ObservableProperty]
    private string currentFileName = string.Empty;

    [ObservableProperty]
    private string currentStatus = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool deleteConfirmationEnabled = true;

    [ObservableProperty]
    private bool isBusy;

    public MainWindowViewModel(IImageCatalogService imageCatalogService, IImageLoaderService imageLoaderService, IFileDeletionService fileDeletionService)
    {
        _imageCatalogService = imageCatalogService;
        _imageLoaderService = imageLoaderService;
        _fileDeletionService = fileDeletionService;
    }

    public bool HasImage => CurrentItem is not null && CurrentBitmap is not null;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmptyState => CurrentSession.Items.Count == 0 && !IsBusy;

    public bool CanClose => CurrentSession.HasItems;

    public bool CanGoPrevious => CurrentSession.CurrentIndex > 0;

    public bool CanGoNext => CurrentSession.CurrentIndex >= 0 && CurrentSession.CurrentIndex < CurrentSession.Items.Count - 1;

    public bool CanDelete => CurrentItem is not null && !IsBusy;

    public ImageEntry? CurrentItem => CurrentSession.CurrentIndex >= 0 && CurrentSession.CurrentIndex < CurrentSession.Items.Count
        ? CurrentSession.Items[CurrentSession.CurrentIndex]
        : null;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousAsync() => NavigateToAsync(CurrentSession.CurrentIndex - 1);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextAsync() => NavigateToAsync(CurrentSession.CurrentIndex + 1);

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task RefreshCurrentAsync() => NavigateToAsync(CurrentSession.CurrentIndex);

    [RelayCommand(CanExecute = nameof(CanClose))]
    private void CloseSession()
    {
        CurrentBitmap?.Dispose();
        CurrentBitmap = null;
        CurrentSession = new BrowserSession(Array.Empty<ImageEntry>(), -1);
        CurrentFileName = string.Empty;
        CurrentStatus = string.Empty;
        ErrorMessage = string.Empty;
        NotifyStateChanged();
    }

    public async Task OpenFilesAsync(IEnumerable<string> filePaths)
    {
        await LoadSessionAsync(_imageCatalogService.CreateFromFiles(filePaths));
    }

    public async Task OpenFolderAsync(string folderPath)
    {
        await LoadSessionAsync(_imageCatalogService.CreateFromDirectory(folderPath));
    }

    public async Task<bool> DeleteCurrentAsync()
    {
        if (CurrentItem is null)
        {
            return false;
        }

        var itemToDelete = CurrentItem;
        IsBusy = true;
        ErrorMessage = string.Empty;

        var result = await _fileDeletionService.DeleteToRecycleBinAsync(itemToDelete, CurrentSession.Items);
        if (!result.Success)
        {
            if (result.CurrentImageDeleted)
            {
                await RemoveDeletedItemAndNavigateAsync(itemToDelete, null, BuildPartialDeleteWarning(result.ErrorMessage));
                return false;
            }

            IsBusy = false;
            ErrorMessage = BuildDeleteFailureMessage(result);
            NotifyStateChanged();
            return false;
        }

        await RemoveDeletedItemAndNavigateAsync(itemToDelete, result.NextIndex, null);
        return true;
    }

    private async Task LoadSessionAsync(BrowserSession session)
    {
        CurrentBitmap?.Dispose();
        CurrentBitmap = null;
        CurrentSession = session;
        ErrorMessage = string.Empty;

        if (!session.HasItems)
        {
            CurrentFileName = "No images found";
            CurrentStatus = string.Empty;
            NotifyStateChanged();
            return;
        }

        await NavigateToAsync(session.CurrentIndex);
    }

    private async Task NavigateToAsync(int index)
    {
        if (index < 0 || index >= CurrentSession.Items.Count)
        {
            NotifyStateChanged();
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var item = CurrentSession.Items[index];
        CurrentBitmap?.Dispose();
        CurrentBitmap = null;

        try
        {
            CurrentBitmap = await _imageLoaderService.LoadAsync(item.FilePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            CurrentSession = new BrowserSession(CurrentSession.Items, index);
            CurrentFileName = item.FileName;
            CurrentStatus = BuildStatus(item, CurrentBitmap, index, CurrentSession.Items.Count);
            IsBusy = false;
            NotifyStateChanged();
        }
    }

    private async Task RemoveDeletedItemAndNavigateAsync(ImageEntry deletedItem, int? nextIndex, string? warningMessage)
    {
        var remainingItems = CurrentSession.Items
            .Where(item => !string.Equals(item.FilePath, deletedItem.FilePath, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        CurrentBitmap?.Dispose();
        CurrentBitmap = null;
        CurrentSession = new BrowserSession(remainingItems, nextIndex ?? -1);
        ErrorMessage = string.Empty;
        IsBusy = false;

        if (CurrentSession.HasItems)
        {
            await NavigateToAsync(CurrentSession.CurrentIndex);
            if (!string.IsNullOrWhiteSpace(warningMessage))
            {
                CurrentStatus = $"{CurrentStatus} · {warningMessage}";
                NotifyStateChanged();
            }
            return;
        }

        CurrentFileName = "All images deleted";
        CurrentStatus = warningMessage ?? string.Empty;
        NotifyStateChanged();
    }

    partial void OnCurrentBitmapChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(HasImage));
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnCurrentSessionChanged(BrowserSession value)
    {
        NotifyStateChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CurrentItem));
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanClose));
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        RefreshCurrentCommand.NotifyCanExecuteChanged();
        CloseSessionCommand.NotifyCanExecuteChanged();
    }

    private static string BuildStatus(ImageEntry item, Bitmap? bitmap, int index, int totalCount)
    {
        var sizeText = bitmap is not null
            ? $"{bitmap.PixelSize.Width} × {bitmap.PixelSize.Height}"
            : item.PixelWidth.HasValue && item.PixelHeight.HasValue
            ? $"{item.PixelWidth} × {item.PixelHeight}"
            : "Unknown size";
        var fileSize = item.FileSizeBytes >= 1024 * 1024
            ? $"{item.FileSizeBytes / 1024d / 1024d:F1} MB"
            : $"{item.FileSizeBytes / 1024d:F0} KB";

        return $"{index + 1}/{totalCount} · {sizeText} · {fileSize}";
    }

    private static string BuildDeleteFailureMessage(DeleteResult result)
    {
        return BuildDeleteFailureMessage(result.ErrorMessage, result.DeletedPaths.Count, false);
    }

    private static string BuildPartialDeleteWarning(string? errorMessage)
    {
        return BuildDeleteFailureMessage(errorMessage, 1, false);
    }

    private static string BuildDeleteFailureMessage(string? errorMessage, int deletedPathCount, bool sessionBecameEmpty)
    {
        var prefix = deletedPathCount > 0
            ? "Some files have been moved to the Recycle Bin, but could not complete the full deletion."
            : "Delete failed.";

        if (sessionBecameEmpty && deletedPathCount > 0)
        {
            prefix = "The current image has been moved to the Recycle Bin, but some associated files failed to delete.";
        }

        return string.IsNullOrWhiteSpace(errorMessage)
            ? prefix
            : $"{prefix} {errorMessage}";
    }
}
