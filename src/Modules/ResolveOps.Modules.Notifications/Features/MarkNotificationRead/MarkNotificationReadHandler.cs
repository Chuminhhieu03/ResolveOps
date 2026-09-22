using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Notifications.Features.MarkNotificationRead;

public class MarkNotificationReadHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public MarkNotificationReadHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MarkNotificationReadResponse>> HandleAsync(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == command.NotificationId && n.UserId == command.UserId, cancellationToken);

        if (notification == null)
        {
            return Result<MarkNotificationReadResponse>.Failure(new DomainError("NOTIFICATION_NOT_FOUND", "Notification not found."));
        }

        notification.MarkAsRead(_timeProvider);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<MarkNotificationReadResponse>.Success(new MarkNotificationReadResponse(
            notification.Id,
            notification.IsRead,
            notification.ReadAtUtc));
    }
}
