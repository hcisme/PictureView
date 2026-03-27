using Avalonia.Controls;
using Avalonia.Interactivity;
using PictureView.Helpers;
using PictureView.ViewModels;

namespace PictureView.Views;

public partial class ContentView : UserControl
{
    public ContentView()
    {
        InitializeComponent();
    }

    private async void OnAddFolderButtonClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folderPaths = await DialogHelper.OpenFolderPicker(this);
            if (folderPaths.Length > 0 && DataContext is MainWindowViewModel viewModel)
            {
                viewModel.AddFolders(folderPaths);
            }
        }
        catch
        {
            // ignored
        }
    }
}