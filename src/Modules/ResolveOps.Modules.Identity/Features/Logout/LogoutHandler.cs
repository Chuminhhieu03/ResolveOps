using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Modules.Identity.Features.Audit;
using ResolveOps.Modules.Identity.Features.Auth;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Identity.Features.Logout;

internal sealed class LogoutHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly AuditService _auditService;
    private readonly TimeProvider _timeProvider;

    public LogoutHandler(
        AppDbContext dbContext,
        ITokenGenerator tokenGenerator,
        AuditService auditService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tokenGenerator = tokenGenerator;
        _auditService = auditService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Success(); // Treat as success if there's no token to log out.
        }

        var hashedToken = _tokenGenerator.HashToken(command.Token);

        var session = await _dbContext.RefreshTokenSessions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TokenHash == hashedToken, cancellationToken);

        if (session != null && !session.IsRevoked)
        {
            session.Revoke(_timeProvider);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _auditService.LogLogout(session.TenantId, session.UserId, session.FamilyId);
        }

        return Result.Success();
    }
}
