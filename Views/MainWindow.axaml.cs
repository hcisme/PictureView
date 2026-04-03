using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PictureView.ViewModels;
using Serilog;

namespace PictureView.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnWindowLoaded;
        Unloaded += OnWindowUnloaded;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModelBase viewModel)
            {
                await viewModel.OnAppearingAsync();
            }
        }
        catch (Exception error)
        {
            Log.Error(error, "窗口加载出现错误");
        }
    }

    private async void OnWindowUnloaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModelBase viewModel)
            {
                await viewModel.OnDisappearingAsync();
            }
        }
        catch (Exception error)
        {
            Log.Error(error, "窗口卸载出现错误");
        }
    }
}