using SharpHook;
using SharpHook.Native;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.Services;

public class HotkeyService : IHotkeyService
{
    private TaskPoolGlobalHook? _hook;
    private HotkeyBinding _binding = new();
    private readonly bool _isMacOS;

    public event EventHandler? HotkeyTriggered;

    public HotkeyService(IPlatformService platform)
    {
        _isMacOS = platform.IsMacOS;
    }

    public async Task StartAsync(HotkeyBinding binding)
    {
        _binding = binding;
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _ = Task.Run(async () =>
        {
            try
            {
                await _hook.RunAsync();
            }
            catch (HookException)
            {
                // Hook was disposed or failed to start
            }
        });
        // Give the hook time to start
        await Task.Delay(200);
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode != (KeyCode)_binding.KeyCode)
            return;

        var mask = e.RawEvent.Mask;

        bool ctrlDown = mask.HasFlag(ModifierMask.LeftCtrl) || mask.HasFlag(ModifierMask.RightCtrl);
        bool shiftDown = mask.HasFlag(ModifierMask.LeftShift) || mask.HasFlag(ModifierMask.RightShift);
        bool altDown = mask.HasFlag(ModifierMask.LeftAlt) || mask.HasFlag(ModifierMask.RightAlt);
        bool metaDown = mask.HasFlag(ModifierMask.LeftMeta) || mask.HasFlag(ModifierMask.RightMeta);

        bool modifierMatch;
        if (_isMacOS)
        {
            // On macOS, "Ctrl" in config means Cmd (Meta)
            modifierMatch = (_binding.Ctrl == metaDown) &&
                            (_binding.Shift == shiftDown) &&
                            (_binding.Alt == altDown);
        }
        else
        {
            modifierMatch = (_binding.Ctrl == ctrlDown) &&
                            (_binding.Shift == shiftDown) &&
                            (_binding.Alt == altDown) &&
                            (_binding.Meta == metaDown);
        }

        if (modifierMatch)
        {
            HotkeyTriggered?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task StopAsync()
    {
        if (_hook is not null)
        {
            _hook.KeyPressed -= OnKeyPressed;
            _hook.Dispose();
            _hook = null;
        }
        await Task.CompletedTask;
    }

    public async Task UpdateBindingAsync(HotkeyBinding binding)
    {
        await StopAsync();
        await StartAsync(binding);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
