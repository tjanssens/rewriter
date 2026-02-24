namespace TextRewriter.Core.Models;

public sealed class RewriteProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public string ModelId { get; set; } = "claude-sonnet-4-20250514";
}
