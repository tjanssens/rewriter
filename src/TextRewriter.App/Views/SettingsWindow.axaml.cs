using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TextRewriter.App.ViewModels;

namespace TextRewriter.App.Views;

public partial class SettingsWindow : Window
{
    // SharpHook 5.x KeyCode values (match Windows VK codes for letters/numbers)
    private static readonly Dictionary<Key, (ushort Code, string Name)> KeyMap = new()
    {
        [Key.A] = (65, "A"), [Key.B] = (66, "B"), [Key.C] = (67, "C"),
        [Key.D] = (68, "D"), [Key.E] = (69, "E"), [Key.F] = (70, "F"),
        [Key.G] = (71, "G"), [Key.H] = (72, "H"), [Key.I] = (73, "I"),
        [Key.J] = (74, "J"), [Key.K] = (75, "K"), [Key.L] = (76, "L"),
        [Key.M] = (77, "M"), [Key.N] = (78, "N"), [Key.O] = (79, "O"),
        [Key.P] = (80, "P"), [Key.Q] = (81, "Q"), [Key.R] = (82, "R"),
        [Key.S] = (83, "S"), [Key.T] = (84, "T"), [Key.U] = (85, "U"),
        [Key.V] = (86, "V"), [Key.W] = (87, "W"), [Key.X] = (88, "X"),
        [Key.Y] = (89, "Y"), [Key.Z] = (90, "Z"),
        [Key.D0] = (48, "0"), [Key.D1] = (49, "1"), [Key.D2] = (50, "2"),
        [Key.D3] = (51, "3"), [Key.D4] = (52, "4"), [Key.D5] = (53, "5"),
        [Key.D6] = (54, "6"), [Key.D7] = (55, "7"), [Key.D8] = (56, "8"),
        [Key.D9] = (57, "9"),
        [Key.F1] = (112, "F1"), [Key.F2] = (113, "F2"), [Key.F3] = (114, "F3"),
        [Key.F4] = (115, "F4"), [Key.F5] = (116, "F5"), [Key.F6] = (117, "F6"),
        [Key.F7] = (118, "F7"), [Key.F8] = (119, "F8"), [Key.F9] = (120, "F9"),
        [Key.F10] = (121, "F10"), [Key.F11] = (122, "F11"), [Key.F12] = (123, "F12"),
        [Key.Space] = (32, "Space"), [Key.Enter] = (10, "Enter"),
        [Key.Tab] = (9, "Tab"),
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
