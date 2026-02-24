using TextRewriter.Core.Models;

namespace TextRewriter.Core.Interfaces;

public interface IRewriteService
{
    Task<RewriteResult> RewriteAsync(string text, RewriteProfile profile, CancellationToken ct = default);
}
