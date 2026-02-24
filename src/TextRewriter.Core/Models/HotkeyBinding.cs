namespace TextRewriter.Core.Models;

public sealed class HotkeyBinding
{
    public ushort KeyCode { get; set; } = 0x0013; // 'R' key
    public bool Ctrl { get; set; } = true;
    public bool Shift { get; set; } = true;
    public bool Alt { get; set; }
    public bool Meta { get; set; }

    public string DisplayName
    {
        get
        {
            var parts = new List<string>();
            if (Ctrl) parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt) parts.Add("Alt");
            if (Meta) parts.Add("Meta");
            parts.Add($"0x{KeyCode:X4}");
            return string.Join("+", parts);
        }
    }
}
