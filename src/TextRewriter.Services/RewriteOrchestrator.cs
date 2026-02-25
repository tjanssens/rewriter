using Microsoft.Extensions.Logging;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.Services;

public class RewriteOrchestrator
{
    private readonly IClipboardService _clipboard;
    private readonly IInputSimulator _input;
    private readonly IRewriteService _rewrite;
    private readonly ISettingsService _settings;
    private readonly ILogger<RewriteOrchestrator> _logger;
    private bool _isProcessing;

    public event EventHandler<bool>? BusyStateChanged;
    public event EventHandler<string>? StatusMessage;

    public RewriteOrchestrator(
        IClipboardService clipboard,
        IInputSimulator input,
        IRewriteService rewrite,
        ISettingsService settings,
        ILogger<RewriteOrchestrator> logger)
    {
        _clipboard = clipboard;
        _input = input;
        _rewrite = rewrite;
        _settings = settings;
        _logger = logger;
    }

    public async Task HandleHotkeyAsync()
    {
        if (_isProcessing)
        {
            _logger.LogDebug("Rewrite already in progress, ignoring hotkey");
            return;
        }

        _isProcessing = true;
        BusyStateChanged?.Invoke(this, true);
        StatusMessage?.Invoke(this, "Herschrijven...");

        string? originalClipboard = null;

        try
        {
            var appSettings = await _settings.LoadAsync();
            var activeProfile = appSettings.Profiles
                .FirstOrDefault(p => p.Id == appSettings.ActiveProfileId)
                ?? appSettings.Profiles.FirstOrDefault();

            if (activeProfile is null)
            {
                StatusMessage?.Invoke(this, "Geen profiel geconfigureerd.");
                return;
            }

            // Step 1: Save current clipboard content
            originalClipboard = await _clipboard.GetTextAsync();

            // Step 2: Wait for hotkey keys to be released
            await Task.Delay(appSettings.KeySimulationDelayMs);

            // Step 3: Simulate Ctrl+C to copy selection
            _input.SimulateCopy();
            await Task.Delay(appSettings.ClipboardDelayMs);

            // Step 4: Read what was copied
            var copiedText = await _clipboard.GetTextAsync();

            // Step 5: If nothing was selected (clipboard unchanged), select all
            if (string.IsNullOrEmpty(copiedText) || copiedText == originalClipboard)
            {
                _logger.LogDebug("No selection detected, selecting all text");
                _input.SimulateSelectAll();
                await Task.Delay(appSettings.KeySimulationDelayMs);
                _input.SimulateCopy();
                await Task.Delay(appSettings.ClipboardDelayMs);
                copiedText = await _clipboard.GetTextAsync();
            }

            if (string.IsNullOrEmpty(copiedText))
            {
                _logger.LogWarning("No text found to rewrite");
                StatusMessage?.Invoke(this, "Geen tekst gevonden om te herschrijven.");
                return;
            }

            _logger.LogDebug("Sending {Length} chars with profile '{Profile}' (model: {Model})", copiedText.Length, activeProfile.Name, activeProfile.ModelId);

            // Step 6: Send to Claude API
            var result = await _rewrite.RewriteAsync(copiedText, activeProfile);

            if (!result.Success)
            {
                _logger.LogWarning("Rewrite failed: {Error}", result.ErrorMessage);
                StatusMessage?.Invoke(this, result.ErrorMessage ?? "Herschrijven mislukt.");
                // Restore original clipboard
                if (originalClipboard is not null)
                    await _clipboard.SetTextAsync(originalClipboard);
                return;
            }

            // Step 7: Put rewritten text on clipboard and paste
            await _clipboard.SetTextAsync(result.RewrittenText!);
            await Task.Delay(appSettings.KeySimulationDelayMs);
            _input.SimulatePaste();

            StatusMessage?.Invoke(this, $"Herschreven met '{activeProfile.Name}'");

            // Step 8: Restore original clipboard after a delay
            await Task.Delay(500);
            if (originalClipboard is not null)
                await _clipboard.SetTextAsync(originalClipboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rewrite orchestration failed");
            StatusMessage?.Invoke(this, $"Fout: {ex.Message}");

            // Try to restore clipboard
            if (originalClipboard is not null)
            {
                try { await _clipboard.SetTextAsync(originalClipboard); }
                catch { /* best effort */ }
            }
        }
        finally
        {
            _isProcessing = false;
            BusyStateChanged?.Invoke(this, false);
        }
    }
}
