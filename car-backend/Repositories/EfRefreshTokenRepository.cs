using Infrastructure;
using Models;

namespace Repositories;

public class EfRefreshTokenRepository : EfRepository<RefreshToken>, IRefreshTokenRepository
{
    private readonly SorchaDbContext _db;

    public EfRefreshTokenRepository(SorchaDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<bool> RevokeIfActiveAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var affected = await _db.RefreshTokens
            .Where(t => t.Token == tokenHash && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ExecuteDeleteAsync();
        // .ExecuteUpdateAsync(s => s
        //     .SetProperty(t => t.IsRevoked, true)
        //     .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);
        return affected > 0;
    }
}
