using Models;

namespace Infrastructure;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<bool> RevokeIfActiveAsync(string tokenHash, CancellationToken cancellationToken = default);
}
