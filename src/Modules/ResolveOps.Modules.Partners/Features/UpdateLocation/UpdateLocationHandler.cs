using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.UpdateLocation;

internal sealed class UpdateLocationHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UpdateLocationHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await _dbContext.Locations
            .FirstOrDefaultAsync(l => l.Id == command.LocationId, cancellationToken);

        if (location is null)
        {
            return DomainError.ResourceNotFound with { Message = "Location not found." };
        }

        // Assign client's concurrency stamp; AppDbContext handles original value and new stamp generation
        location.ConcurrencyStamp = command.ConcurrencyStamp;

        location.Update(
            command.Name,
            command.AddressLine1,
            command.AddressLine2,
            command.City,
            command.Region,
            command.PostalCode,
            command.CountryCode,
            command.Timezone,
            command.Latitude,
            command.Longitude,
            _timeProvider);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict with
            {
                Message = "The location was updated by another user. Please refresh and try again."
            };
        }
    }
}
