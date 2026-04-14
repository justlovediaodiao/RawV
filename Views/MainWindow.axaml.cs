using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RawV.ViewModels;

namespace RawV.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private async void OnOpenFilesClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图片文件",
            AllowMultiple = true
        });

        if (files.Count == 0)
        {
            return;
        }

        await ViewModel.OpenFilesAsync(files.Select(static file => file.TryGetLocalPath()).OfType<string>());
    }

    private async void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择图片文件夹",
            AllowMultiple = false
        });

        var folderPath = folders.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        await ViewModel.OpenFolderAsync(folderPath);
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        await DeleteCurrentAsync();
    }

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Left or Key.Up)
        {
            if (ViewModel.PreviousCommand.CanExecute(null))
            {
                await ViewModel.PreviousCommand.ExecuteAsync(null);
            }

            e.Handled = true;
            return;
        }

        if (e.Key is Key.Right or Key.Down)
        {
            if (ViewModel.NextCommand.CanExecute(null))
            {
                await ViewModel.NextCommand.ExecuteAsync(null);
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete)
        {
            await DeleteCurrentAsync();
            e.Handled = true;
        }
    }

    private async Task DeleteCurrentAsync()
    {
        if (!ViewModel.CanDelete)
        {
            return;
        }

        if (ViewModel.DeleteConfirmationEnabled)
        {
            var dialog = new DeleteConfirmationWindow();
            var confirmed = await dialog.ShowDialog<bool>(this);
            if (!confirmed)
            {
                return;
            }
        }

        await ViewModel.DeleteCurrentAsync();
    }
}
