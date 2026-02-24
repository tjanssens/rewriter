using TextRewriter.Core.Models;

namespace TextRewriter.Core.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
}
