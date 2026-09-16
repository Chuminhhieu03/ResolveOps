using ResolveOps.Domain.Messaging;

namespace ResolveOps.Application.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetRecordAsync(Guid tenantId, string scope, string key, CancellationToken cancellationToken);
    Task SaveRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken);
}
