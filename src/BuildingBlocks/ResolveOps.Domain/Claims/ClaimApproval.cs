using System;

namespace ResolveOps.Domain.Claims;

public sealed class ClaimApproval : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ClaimId { get; private set; }
    public string ApprovalType { get; private set; } = string.Empty;
    public string Status { get; private set; } = ApprovalStatus.Pending;
    public Guid RequestedBy { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public Guid? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? DecisionNote { get; private set; }
    public string ClaimVersion { get; private set; } = string.Empty;

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private ClaimApproval() { } // EF Core

    public static ClaimApproval CreateSubmissionRequest(
        Guid tenantId,
        Guid claimId,
        Guid requestedBy,
        string claimVersion,
        TimeProvider timeProvider)
    {
        return new ClaimApproval
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClaimId = claimId,
            ApprovalType = Claims.ApprovalType.Submission,
            Status = ApprovalStatus.Pending,
            RequestedBy = requestedBy,
            RequestedAtUtc = timeProvider.GetUtcNow(),
            ClaimVersion = claimVersion
        };
    }

    public void Approve(Guid decidedBy, string? note, TimeProvider timeProvider)
    {
        DecidedBy = decidedBy;
        DecidedAtUtc = timeProvider.GetUtcNow();
        DecisionNote = note;
        Status = ApprovalStatus.Approved;
    }

    public void Reject(Guid decidedBy, string reason, TimeProvider timeProvider)
    {
        DecidedBy = decidedBy;
        DecidedAtUtc = timeProvider.GetUtcNow();
        DecisionNote = reason;
        Status = ApprovalStatus.Rejected;
    }
}
