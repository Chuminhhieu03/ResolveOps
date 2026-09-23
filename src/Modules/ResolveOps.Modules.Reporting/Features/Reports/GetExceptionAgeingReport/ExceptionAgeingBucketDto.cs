using System;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetExceptionAgeingReport;

public sealed record ExceptionAgeingBucketDto(
    Guid? CarrierId,
    string CarrierName,
    string ExceptionType,
    int Bucket0To24h,
    int Bucket24To48h,
    int Bucket48To72h,
    int Bucket3To7d,
    int BucketGreaterThan7d,
    int TotalOpenCases);
