using TextRewriter.Core.Interfaces;

namespace TextRewriter.Services;

public class TextClipboardService : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        return await Task.Run(() => TextCopy.ClipboardService.GetText());
    }

    public async Task SetTextAsync(string text)
    {
        await Task.Run(() => TextCopy.ClipboardService.SetText(text));
    }
}
