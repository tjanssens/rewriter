namespace TextRewriter.Core.Models;

public sealed class AppSettings
{
    public List<RewriteProfile> Profiles { get; set; } = [];
    public string ActiveProfileId { get; set; } = "";
    public HotkeyBinding Hotkey { get; set; } = new();
    public string? ApiKey { get; set; }
    public bool ShowNotifications { get; set; } = true;
    public int ClipboardDelayMs { get; set; } = 150;
    public int KeySimulationDelayMs { get; set; } = 50;
}
