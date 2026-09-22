using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.ApproveForSubmission;

public class ApproveForSubmissionHandler
{
    private const decimal _defaultHighValueThreshold = 10_000_000m; // Spec §26.10

    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ApproveForSubmissionHandler(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ApproveForSubmissionResponse>> HandleAsync(
        ApproveForSubmissionCommand command,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.Approvals)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<ApproveForSubmissionResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        // Determine tenant threshold
        var threshold = _defaultHighValueThreshold;
        var tenantSettings = await _dbContext.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == claim.TenantId, cancellationToken);

        if (tenantSettings != null && !string.IsNullOrWhiteSpace(tenantSettings.SettingsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(tenantSettings.SettingsJson);
                if (doc.RootElement.TryGetProperty("claimApprovalThreshold", out var thresholdProp) &&
                    thresholdProp.TryGetDecimal(out var parsedThreshold))
                {
                    threshold = parsedThreshold;
                }
            }
            catch (JsonException)
            {
                // Fall back to default
            }
        }

        var approvalResult = claim.ApproveForSubmission(command.ApproverId, threshold, command.Note, _timeProvider);
        if (approvalResult.IsFailure)
        {
            return Result<ApproveForSubmissionResponse>.Failure(approvalResult.Error);
        }

        ClaimMetrics.ClaimsApprovedTotal.Add(1);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ApproveForSubmissionResponse>.Success(new ApproveForSubmissionResponse(
            claim.Id,
            claim.Status,
            claim.ApprovedForSubmissionBy!.Value,
            claim.ApprovedForSubmissionAtUtc!.Value));
    }
}
