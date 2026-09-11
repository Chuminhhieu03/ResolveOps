namespace ResolveOps.Messaging;

/// <summary>
/// Writes integration events to the outbox (spec §17 / Phase 5).
///
/// The outbox message is added to the EF Core change tracker but NOT saved.
/// The handler must call <c>SaveChangesAsync</c> after calling this method,
/// ensuring the outbox write and the business aggregate change are committed
/// in a single database transaction.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Appends an outbox message for the given integration event.
    /// Does not save; the caller must save within the same transaction.
    /// </summary>
    void Write(IIntegrationEvent integrationEvent, string? causationId = null);
}
