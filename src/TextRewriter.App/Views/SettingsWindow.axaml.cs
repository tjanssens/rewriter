using Avalonia.Controls;
using Avalonia.Interactivity;
using TextRewriter.App.ViewModels;

namespace TextRewriter.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnAddProfile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.AddProfile();
    }

    private void OnRemoveProfile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.RemoveProfile();
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.SaveAsync();
            Close();
        }
    }
}
