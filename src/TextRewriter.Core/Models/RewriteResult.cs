namespace TextRewriter.Core.Models;

public sealed class RewriteResult
{
    public bool Success { get; set; }
    public string? RewrittenText { get; set; }
    public string? ErrorMessage { get; set; }
}
