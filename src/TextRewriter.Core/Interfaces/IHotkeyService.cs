using TextRewriter.Core.Models;

namespace TextRewriter.Core.Interfaces;

public interface IHotkeyService : IAsyncDisposable
{
    event EventHandler? HotkeyTriggered;
    Task StartAsync(HotkeyBinding binding);
    Task StopAsync();
    Task UpdateBindingAsync(HotkeyBinding binding);
}
