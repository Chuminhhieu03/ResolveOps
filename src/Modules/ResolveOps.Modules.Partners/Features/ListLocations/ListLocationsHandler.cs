using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Modules.Partners.Features.GetLocation;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.ListLocations;

internal sealed class ListLocationsHandler
{
    private readonly AppDbContext _dbContext;

    public ListLocationsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListLocationsResponse>> HandleAsync(
        string? countryCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);
        var clampedPage = Math.Max(1, page);

        var dbQuery = _dbContext.Locations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(l => l.CountryCode == normalizedCountryCode);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderBy(l => l.Code)
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .Select(l => new LocationSummary(l.Id, l.Code, l.Name, l.City, l.CountryCode))
            .ToListAsync(cancellationToken);

        return new ListLocationsResponse(items, totalCount, clampedPage, clampedPageSize);
    }
}

public sealed record LocationSummary(Guid Id, string Code, string Name, string? City, string CountryCode);
public sealed record ListLocationsResponse(IReadOnlyList<LocationSummary> Items, int TotalCount, int Page, int PageSize);
