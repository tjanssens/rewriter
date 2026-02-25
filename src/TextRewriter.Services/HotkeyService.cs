using Microsoft.Extensions.Logging;
using SharpHook;
using SharpHook.Native;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.Services;

public class HotkeyService : IHotkeyService
{
    private TaskPoolGlobalHook? _hook;
    private Task? _hookTask;
    private HotkeyBinding _binding = new();
    private readonly bool _isMacOS;
    private readonly ILogger<HotkeyService> _logger;

    public event EventHandler? HotkeyTriggered;

    public HotkeyService(IPlatformService platform, ILogger<HotkeyService> logger)
    {
        _isMacOS = platform.IsMacOS;
        _logger = logger;
    }

    public async Task StartAsync(HotkeyBinding binding)
    {
        _binding = binding;
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hookTask = Task.Run(async () =>
        {
            try
            {
                await _hook.RunAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Global hook failed to run");
            }
        });
        await Task.Delay(500);
        _logger.LogInformation("Global hook listening for {Hotkey}", binding.DisplayName);
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
            if (_hookTask is not null)
            {
                await _hookTask;
                _hookTask = null;
            }
            _hook = null;
        }
    }

    public Task UpdateBindingAsync(HotkeyBinding binding)
    {
        _binding = binding;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
