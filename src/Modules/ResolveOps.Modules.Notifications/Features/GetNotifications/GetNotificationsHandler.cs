using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Notifications.Features.GetNotifications;

public class GetNotificationsHandler
{
    private readonly AppDbContext _dbContext;

    public GetNotificationsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetNotificationsResponse>> HandleAsync(
        GetNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == query.UserId);

        if (query.IsRead.HasValue)
        {
            dbQuery = dbQuery.Where(n => n.IsRead == query.IsRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.NotificationClass))
        {
            dbQuery = dbQuery.Where(n => n.NotificationClass == query.NotificationClass.Trim());
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.NotificationClass,
                n.Channel,
                n.Title,
                n.Message,
                n.DataJson,
                n.IsRead,
                n.ReadAtUtc,
                n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return Result<GetNotificationsResponse>.Success(new GetNotificationsResponse(
            items,
            totalCount,
            query.Page,
            query.PageSize,
            totalPages));
    }
}
