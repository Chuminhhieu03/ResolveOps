using System.Collections.Generic;

namespace ResolveOps.Domain.Claims;

public static class ApprovalType
{
    public const string Submission = "Submission";
    public const string WriteOff = "WriteOff";

    public static readonly IReadOnlyList<string> All =
    [
        Submission,
        WriteOff
    ];
}
