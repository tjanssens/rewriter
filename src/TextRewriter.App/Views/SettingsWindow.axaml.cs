using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TextRewriter.App.ViewModels;

namespace TextRewriter.App.Views;

public partial class SettingsWindow : Window
{
    private static readonly Dictionary<Key, (ushort Code, string Name)> KeyMap = new()
    {
        [Key.A] = (0x001E, "A"), [Key.B] = (0x0030, "B"), [Key.C] = (0x002E, "C"),
        [Key.D] = (0x0020, "D"), [Key.E] = (0x0012, "E"), [Key.F] = (0x0021, "F"),
        [Key.G] = (0x0022, "G"), [Key.H] = (0x0023, "H"), [Key.I] = (0x0017, "I"),
        [Key.J] = (0x0024, "J"), [Key.K] = (0x0025, "K"), [Key.L] = (0x0026, "L"),
        [Key.M] = (0x0032, "M"), [Key.N] = (0x0031, "N"), [Key.O] = (0x0018, "O"),
        [Key.P] = (0x0019, "P"), [Key.Q] = (0x0010, "Q"), [Key.R] = (0x0013, "R"),
        [Key.S] = (0x001F, "S"), [Key.T] = (0x0014, "T"), [Key.U] = (0x0016, "U"),
        [Key.V] = (0x002F, "V"), [Key.W] = (0x0011, "W"), [Key.X] = (0x002D, "X"),
        [Key.Y] = (0x0015, "Y"), [Key.Z] = (0x002C, "Z"),
        [Key.D0] = (0x000B, "0"), [Key.D1] = (0x0002, "1"), [Key.D2] = (0x0003, "2"),
        [Key.D3] = (0x0004, "3"), [Key.D4] = (0x0005, "4"), [Key.D5] = (0x0006, "5"),
        [Key.D6] = (0x0007, "6"), [Key.D7] = (0x0008, "7"), [Key.D8] = (0x0009, "8"),
        [Key.D9] = (0x000A, "9"),
        [Key.F1] = (0x003B, "F1"), [Key.F2] = (0x003C, "F2"), [Key.F3] = (0x003D, "F3"),
        [Key.F4] = (0x003E, "F4"), [Key.F5] = (0x003F, "F5"), [Key.F6] = (0x0040, "F6"),
        [Key.F7] = (0x0041, "F7"), [Key.F8] = (0x0042, "F8"), [Key.F9] = (0x0043, "F9"),
        [Key.F10] = (0x0044, "F10"), [Key.F11] = (0x0057, "F11"), [Key.F12] = (0x0058, "F12"),
        [Key.Space] = (0x0039, "Space"), [Key.Enter] = (0x001C, "Enter"),
        [Key.Tab] = (0x000F, "Tab"),
    };

    public SettingsWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            await vm.LoadModelsAsync();
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

    private void OnRecordHotkey(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.IsRecordingHotkey = true;
            KeyDown += OnHotkeyKeyDown;
        }
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin)
            return;

        KeyDown -= OnHotkeyKeyDown;

        if (DataContext is not SettingsViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.IsRecordingHotkey = false;
            e.Handled = true;
            return;
        }

        if (KeyMap.TryGetValue(e.Key, out var mapped))
        {
            var mods = e.KeyModifiers;
            vm.UpdateHotkey(
                mapped.Code,
                mapped.Name,
                ctrl: mods.HasFlag(KeyModifiers.Control),
                shift: mods.HasFlag(KeyModifiers.Shift),
                alt: mods.HasFlag(KeyModifiers.Alt),
                meta: mods.HasFlag(KeyModifiers.Meta));
        }
        else
        {
            vm.IsRecordingHotkey = false;
        }

        e.Handled = true;
    }
}
