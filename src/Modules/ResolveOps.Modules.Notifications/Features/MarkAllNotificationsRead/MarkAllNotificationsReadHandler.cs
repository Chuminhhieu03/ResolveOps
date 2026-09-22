using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Notifications.Features.MarkAllNotificationsRead;

public class MarkAllNotificationsReadHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public MarkAllNotificationsReadHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MarkAllNotificationsReadResponse>> HandleAsync(
        MarkAllNotificationsReadCommand command,
        CancellationToken cancellationToken)
    {
        var unreadNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == command.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(_timeProvider);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<MarkAllNotificationsReadResponse>.Success(
            new MarkAllNotificationsReadResponse(true, unreadNotifications.Count));
    }
}
