using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.ListCustomers;

internal sealed class ListCustomersHandler
{
    private readonly AppDbContext _dbContext;

    public ListCustomersHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListCustomersResponse>> HandleAsync(
        string? priorityFilter,
        string? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);
        var clampedPage = Math.Max(1, page);

        var dbQuery = _dbContext.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(priorityFilter))
        {
            dbQuery = dbQuery.Where(c => c.Priority == priorityFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            dbQuery = dbQuery.Where(c => c.Status == statusFilter);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderBy(c => c.Code)
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .Select(c => new CustomerSummary(c.Id, c.Code, c.Name, c.Priority, c.Status))
            .ToListAsync(cancellationToken);

        return new ListCustomersResponse(items, totalCount, clampedPage, clampedPageSize);
    }
}

public sealed record CustomerSummary(Guid Id, string Code, string Name, string Priority, string Status);
public sealed record ListCustomersResponse(IReadOnlyList<CustomerSummary> Items, int TotalCount, int Page, int PageSize);
