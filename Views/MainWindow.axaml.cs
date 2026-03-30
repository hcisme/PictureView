using Avalonia.Controls;

namespace PictureView.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide(); 
        base.OnClosing(e);
    }
}