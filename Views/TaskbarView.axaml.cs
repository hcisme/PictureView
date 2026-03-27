using Avalonia.Controls;
using Avalonia.Interactivity;
using PictureView.ViewModels;

namespace PictureView.Views;

public partial class TaskbarView : UserControl
{
    public TaskbarView()
    {
        InitializeComponent();
    }

    /**
     * 打开设置 Modal
     */
    private void OnSettingsButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.IsSettingsOpen = true;
        }
    }
}