namespace ResolveOps.Domain.Documents;

/// <summary>
/// Evidence type constants (spec §8.6, §10.4, §15.9).
///
/// These map to the evidence_type column in evidence_documents and
/// evidence_requirements. AllowedMimeTypes enforces the MIME-type allowlist per
/// evidence category — file extension is NOT trusted (spec §10.4 invariant 4).
/// </summary>
public static class EvidenceType
{
    public const string SignedPOD = "SignedPOD";
    public const string DamagePhotos = "DamagePhotos";
    public const string CommercialInvoice = "CommercialInvoice";
    public const string PackingList = "PackingList";
    public const string InspectionReport = "InspectionReport";
    public const string CarrierNotice = "CarrierNotice";
    public const string RepairEstimate = "RepairEstimate";
    public const string SalvageReceipt = "SalvageReceipt";
    public const string WeightCertificate = "WeightCertificate";
    public const string Other = "Other";

    private static readonly HashSet<string> _all =
    [
        SignedPOD, DamagePhotos, CommercialInvoice, PackingList,
        InspectionReport, CarrierNotice, RepairEstimate, SalvageReceipt,
        WeightCertificate, Other,
    ];

    public static bool IsValid(string? evidenceType) =>
        !string.IsNullOrWhiteSpace(evidenceType) && _all.Contains(evidenceType);

    /// <summary>
    /// Allowed MIME types per evidence category.
    /// File extension is never authoritative — content type from this map is used
    /// to validate the client-supplied contentType field (spec §10.4 invariant 4).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedMimeTypes =
        new Dictionary<string, IReadOnlySet<string>>
        {
            [SignedPOD] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/tiff",
            },
            [DamagePhotos] = new HashSet<string>
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/tiff",
                "video/mp4",
            },
            [CommercialInvoice] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/msword",
            },
            [PackingList] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-excel",
            },
            [InspectionReport] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            },
            [CarrierNotice] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "message/rfc822",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            },
            [RepairEstimate] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            },
            [SalvageReceipt] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
            },
            [WeightCertificate] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
            },
            [Other] = new HashSet<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/tiff",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-excel",
            },
        };

    /// <summary>Maximum file size in bytes (25 MB, spec §16.9).</summary>
    public const long MaxFileSizeBytes = 25L * 1024 * 1024;

    /// <summary>
    /// Returns true when the given MIME type is allowed for the evidence type.
    /// </summary>
    public static bool IsMimeTypeAllowed(string evidenceType, string mimeType) =>
        AllowedMimeTypes.TryGetValue(evidenceType, out var allowed) &&
        allowed.Contains(mimeType.ToLowerInvariant().Trim());
}
