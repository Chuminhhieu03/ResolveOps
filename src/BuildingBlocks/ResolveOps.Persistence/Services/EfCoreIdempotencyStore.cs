using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Idempotency;
using ResolveOps.Domain.Messaging;

namespace ResolveOps.Persistence.Services;

public sealed class EfCoreIdempotencyStore : IIdempotencyStore
{
    private readonly AppDbContext _dbContext;

    public EfCoreIdempotencyStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyRecord?> GetRecordAsync(
        Guid tenantId,
        string scope,
        string key,
        CancellationToken cancellationToken)
    {
        return await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.Scope == scope && r.IdempotencyKey == key,
                cancellationToken);
    }

    public async Task SaveRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        _dbContext.IdempotencyRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
