namespace TextRewriter.Core.Interfaces;

public interface IAuthService
{
    Task<string?> GetAccessTokenAsync();
    bool IsAuthenticated { get; }
}
