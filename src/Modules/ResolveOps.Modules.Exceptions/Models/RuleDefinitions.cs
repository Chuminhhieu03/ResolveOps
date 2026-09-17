namespace ResolveOps.Modules.Exceptions.Models;

public sealed class PickupDelayRuleDefinition
{
    public int ToleranceMinutes { get; set; } = 30;
}

public sealed class InTransitDelayRuleDefinition
{
    public int ToleranceMinutes { get; set; } = 60;
    public bool TriggerOnCarrierDelayEvent { get; set; } = true;
}

public sealed class DamageRuleDefinition
{
    public string[] DamageKeywords { get; set; } =
    [
        "damage", "damaged", "broken", "wet", "crushed", "tampered", "destroyed", "leak", "leakage", "torn"
    ];

    public string[] TriggerEventTypes { get; set; } = ["Damaged", "Exception"];
}

public sealed class SeverityDefinition
{
    public decimal HighValueThreshold { get; set; } = 5000m;
    public decimal CriticalValueThreshold { get; set; } = 25000m;
    public int HighDelayMinutesThreshold { get; set; } = 240;
    public int CriticalDelayMinutesThreshold { get; set; } = 1440;
    public string DefaultSeverity { get; set; } = "Medium";
}

public sealed class AssignmentDefinition
{
    public string DefaultTeamCode { get; set; } = "OPS-NORTH";
    public Dictionary<string, string> SeverityTeamOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
