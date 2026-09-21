using System.Collections.Generic;
using System.Linq;

namespace ResolveOps.Domain.Claims;

public static class ClaimStatus
{
    public const string Draft = "Draft";
    public const string EvidencePending = "EvidencePending";
    public const string ReadyForReview = "ReadyForReview";
    public const string ApprovedForSubmission = "ApprovedForSubmission";
    public const string Submitted = "Submitted";
    public const string Acknowledged = "Acknowledged";
    public const string MoreInformationRequested = "MoreInformationRequested";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string PartiallyApproved = "PartiallyApproved";
    public const string Denied = "Denied";
    public const string Appealed = "Appealed";
    public const string SettlementPending = "SettlementPending";
    public const string Paid = "Paid";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        EvidencePending,
        ReadyForReview,
        ApprovedForSubmission,
        Submitted,
        Acknowledged,
        MoreInformationRequested,
        UnderReview,
        Approved,
        PartiallyApproved,
        Denied,
        Appealed,
        SettlementPending,
        Paid,
        Closed,
        Cancelled
    ];
}
