using System.Collections.Generic;

namespace ResolveOps.Domain.Claims;

public static class LossComponentType
{
    public const string ProductValue = "ProductValue";
    public const string FreightCost = "FreightCost";
    public const string TaxesAndDuties = "TaxesAndDuties";
    public const string AssessmentFee = "AssessmentFee";
    public const string MitigationCost = "MitigationCost";
    public const string LegalFee = "LegalFee";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
    [
        ProductValue,
        FreightCost,
        TaxesAndDuties,
        AssessmentFee,
        MitigationCost,
        LegalFee,
        Other
    ];
}
