using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PictureView.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    public virtual Task OnDisappearingAsync() => Task.CompletedTask;
}
