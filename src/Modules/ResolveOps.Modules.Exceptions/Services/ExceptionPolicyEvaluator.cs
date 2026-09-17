using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Shipments;
using ResolveOps.Domain.Tracking;
using ResolveOps.Modules.Exceptions.Models;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Services;

public sealed record DetectionCandidate(
    bool IsViolation,
    string ExceptionType,
    string Fingerprint,
    string BusinessKey,
    Guid PolicyId,
    int PolicyVersionNumber,
    string Severity,
    int? SeverityScore,
    string OwnerTeamCode,
    decimal FinancialExposure,
    string ExposureCurrency,
    string Summary,
    Guid? TrackingEventId = null);

public interface IExceptionPolicyEvaluator
{
    Task<DetectionCandidate?> EvaluatePickupDelayAsync(
        Shipment shipment,
        DateTimeOffset evaluationTimeUtc,
        CancellationToken cancellationToken);

    Task<DetectionCandidate?> EvaluateInTransitDelayAsync(
        Shipment shipment,
        DateTimeOffset evaluationTimeUtc,
        CancellationToken cancellationToken);

    Task<DetectionCandidate?> EvaluateTrackingEventAsync(
        TrackingEvent trackingEvent,
        Shipment shipment,
        CancellationToken cancellationToken);
}

public sealed class ExceptionPolicyEvaluator : IExceptionPolicyEvaluator
{
    private readonly AppDbContext _dbContext;
    private readonly IExceptionFingerprintGenerator _fingerprintGenerator;
    private readonly ISeverityCalculator _severityCalculator;
    private readonly IAssignmentEvaluator _assignmentEvaluator;

    public ExceptionPolicyEvaluator(
        AppDbContext dbContext,
        IExceptionFingerprintGenerator fingerprintGenerator,
        ISeverityCalculator severityCalculator,
        IAssignmentEvaluator assignmentEvaluator)
    {
        _dbContext = dbContext;
        _fingerprintGenerator = fingerprintGenerator;
        _severityCalculator = severityCalculator;
        _assignmentEvaluator = assignmentEvaluator;
    }

    public async Task<DetectionCandidate?> EvaluatePickupDelayAsync(
        Shipment shipment,
        DateTimeOffset evaluationTimeUtc,
        CancellationToken cancellationToken)
    {
        if (shipment.ActualPickupAtUtc.HasValue ||
            string.Equals(shipment.Status, ShipmentStatus.Cancelled, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(shipment.Status, ShipmentStatus.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var policy = await GetActivePolicyAsync(shipment.TenantId, ExceptionType.PickupDelay, cancellationToken);
        var rule = ParsePickupRule(policy?.RuleDefinitionJson);

        var thresholdTime = shipment.PlannedPickupAtUtc.AddMinutes(rule.ToleranceMinutes);
        if (evaluationTimeUtc < thresholdTime)
        {
            return null; // Within tolerance
        }

        var delayMinutes = (int)(evaluationTimeUtc - shipment.PlannedPickupAtUtc).TotalMinutes;
        var policyVersion = policy?.VersionNumber ?? 1;
        var policyId = policy?.Id ?? Guid.Empty;

        var businessKey = shipment.PlannedPickupAtUtc.ToString("O");
        var fingerprint = _fingerprintGenerator.Generate(
            shipment.TenantId,
            shipment.Id,
            null,
            ExceptionType.PickupDelay,
            businessKey,
            policyVersion);

        var severityResult = _severityCalculator.Calculate(
            policy?.SeverityDefinitionJson,
            shipment.DeclaredValue,
            delayMinutes);

        var ownerTeam = _assignmentEvaluator.SelectOwnerTeam(
            policy?.AssignmentDefinitionJson,
            severityResult.Severity,
            ExceptionType.PickupDelay);

        var summary = $"Shipment not confirmed picked up within tolerance of {rule.ToleranceMinutes}m (planned: {shipment.PlannedPickupAtUtc:u}, delay: {delayMinutes}m).";

        return new DetectionCandidate(
            IsViolation: true,
            ExceptionType: ExceptionType.PickupDelay,
            Fingerprint: fingerprint,
            BusinessKey: businessKey,
            PolicyId: policyId,
            PolicyVersionNumber: policyVersion,
            Severity: severityResult.Severity,
            SeverityScore: severityResult.SeverityScore,
            OwnerTeamCode: ownerTeam,
            FinancialExposure: shipment.DeclaredValue ?? 0m,
            ExposureCurrency: shipment.DeclaredValueCurrency ?? "USD",
            Summary: summary);
    }

    public async Task<DetectionCandidate?> EvaluateInTransitDelayAsync(
        Shipment shipment,
        DateTimeOffset evaluationTimeUtc,
        CancellationToken cancellationToken)
    {
        if (shipment.ActualDeliveryAtUtc.HasValue ||
            string.Equals(shipment.Status, ShipmentStatus.Cancelled, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(shipment.Status, ShipmentStatus.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var policy = await GetActivePolicyAsync(shipment.TenantId, ExceptionType.InTransitDelay, cancellationToken);
        var rule = ParseInTransitRule(policy?.RuleDefinitionJson);

        var thresholdTime = shipment.PlannedDeliveryAtUtc.AddMinutes(rule.ToleranceMinutes);
        if (evaluationTimeUtc < thresholdTime)
        {
            return null; // Within tolerance
        }

        var delayMinutes = (int)(evaluationTimeUtc - shipment.PlannedDeliveryAtUtc).TotalMinutes;
        var policyVersion = policy?.VersionNumber ?? 1;
        var policyId = policy?.Id ?? Guid.Empty;

        var businessKey = shipment.PlannedDeliveryAtUtc.ToString("O");
        var fingerprint = _fingerprintGenerator.Generate(
            shipment.TenantId,
            shipment.Id,
            null,
            ExceptionType.InTransitDelay,
            businessKey,
            policyVersion);

        var severityResult = _severityCalculator.Calculate(
            policy?.SeverityDefinitionJson,
            shipment.DeclaredValue,
            delayMinutes);

        var ownerTeam = _assignmentEvaluator.SelectOwnerTeam(
            policy?.AssignmentDefinitionJson,
            severityResult.Severity,
            ExceptionType.InTransitDelay);

        var summary = $"Shipment missed planned delivery commitment within tolerance of {rule.ToleranceMinutes}m (planned: {shipment.PlannedDeliveryAtUtc:u}, delay: {delayMinutes}m).";

        return new DetectionCandidate(
            IsViolation: true,
            ExceptionType: ExceptionType.InTransitDelay,
            Fingerprint: fingerprint,
            BusinessKey: businessKey,
            PolicyId: policyId,
            PolicyVersionNumber: policyVersion,
            Severity: severityResult.Severity,
            SeverityScore: severityResult.SeverityScore,
            OwnerTeamCode: ownerTeam,
            FinancialExposure: shipment.DeclaredValue ?? 0m,
            ExposureCurrency: shipment.DeclaredValueCurrency ?? "USD",
            Summary: summary);
    }

    public async Task<DetectionCandidate?> EvaluateTrackingEventAsync(
        TrackingEvent trackingEvent,
        Shipment shipment,
        CancellationToken cancellationToken)
    {
        // 1. Check for Damage
        var damagePolicy = await GetActivePolicyAsync(shipment.TenantId, ExceptionType.Damage, cancellationToken);
        var damageRule = ParseDamageRule(damagePolicy?.RuleDefinitionJson);

        var isDamageType = damageRule.TriggerEventTypes.Contains(trackingEvent.EventType, StringComparer.OrdinalIgnoreCase);
        var hasDamageText = false;
        var textToScan = $"{trackingEvent.EventCode} {trackingEvent.LocationText}".ToLowerInvariant();

        foreach (var keyword in damageRule.DamageKeywords)
        {
            if (textToScan.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                hasDamageText = true;
                break;
            }
        }

        if (isDamageType || hasDamageText)
        {
            var policyVersion = damagePolicy?.VersionNumber ?? 1;
            var policyId = damagePolicy?.Id ?? Guid.Empty;
            var businessKey = trackingEvent.ExternalEventId ?? trackingEvent.Id.ToString("D");

            var fingerprint = _fingerprintGenerator.Generate(
                shipment.TenantId,
                shipment.Id,
                trackingEvent.ShipmentLegId,
                ExceptionType.Damage,
                businessKey,
                policyVersion);

            var severityResult = _severityCalculator.Calculate(
                damagePolicy?.SeverityDefinitionJson,
                shipment.DeclaredValue,
                delayMinutes: 0,
                initialSeverityHint: ExceptionSeverity.High);

            var ownerTeam = _assignmentEvaluator.SelectOwnerTeam(
                damagePolicy?.AssignmentDefinitionJson,
                severityResult.Severity,
                ExceptionType.Damage);

            var summary = $"Carrier event reported damage or tampering: {trackingEvent.EventType} ({trackingEvent.EventCode ?? "NoCode"}) at {trackingEvent.LocationText ?? "transit location"}.";

            return new DetectionCandidate(
                IsViolation: true,
                ExceptionType: ExceptionType.Damage,
                Fingerprint: fingerprint,
                BusinessKey: businessKey,
                PolicyId: policyId,
                PolicyVersionNumber: policyVersion,
                Severity: severityResult.Severity,
                SeverityScore: severityResult.SeverityScore,
                OwnerTeamCode: ownerTeam,
                FinancialExposure: shipment.DeclaredValue ?? 0m,
                ExposureCurrency: shipment.DeclaredValueCurrency ?? "USD",
                Summary: summary,
                TrackingEventId: trackingEvent.Id);
        }

        // 2. Check for InTransitDelay event
        if (string.Equals(trackingEvent.EventType, "Delayed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trackingEvent.EventType, "Exception", StringComparison.OrdinalIgnoreCase))
        {
            var delayPolicy = await GetActivePolicyAsync(shipment.TenantId, ExceptionType.InTransitDelay, cancellationToken);
            var policyVersion = delayPolicy?.VersionNumber ?? 1;
            var policyId = delayPolicy?.Id ?? Guid.Empty;
            var businessKey = trackingEvent.ExternalEventId ?? trackingEvent.Id.ToString("D");

            var fingerprint = _fingerprintGenerator.Generate(
                shipment.TenantId,
                shipment.Id,
                trackingEvent.ShipmentLegId,
                ExceptionType.InTransitDelay,
                businessKey,
                policyVersion);

            var severityResult = _severityCalculator.Calculate(
                delayPolicy?.SeverityDefinitionJson,
                shipment.DeclaredValue,
                delayMinutes: 60);

            var ownerTeam = _assignmentEvaluator.SelectOwnerTeam(
                delayPolicy?.AssignmentDefinitionJson,
                severityResult.Severity,
                ExceptionType.InTransitDelay);

            var summary = $"Carrier reported transit delay event: {trackingEvent.EventType} ({trackingEvent.EventCode ?? "Delay"}) at {trackingEvent.LocationText ?? "transit"}.";

            return new DetectionCandidate(
                IsViolation: true,
                ExceptionType: ExceptionType.InTransitDelay,
                Fingerprint: fingerprint,
                BusinessKey: businessKey,
                PolicyId: policyId,
                PolicyVersionNumber: policyVersion,
                Severity: severityResult.Severity,
                SeverityScore: severityResult.SeverityScore,
                OwnerTeamCode: ownerTeam,
                FinancialExposure: shipment.DeclaredValue ?? 0m,
                ExposureCurrency: shipment.DeclaredValueCurrency ?? "USD",
                Summary: summary,
                TrackingEventId: trackingEvent.Id);
        }

        return null;
    }

    private async Task<ExceptionPolicy?> GetActivePolicyAsync(
        Guid tenantId,
        string exceptionType,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ExceptionPolicies
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId &&
                        p.ExceptionType == exceptionType &&
                        p.Status == PolicyStatus.Active)
            .OrderByDescending(p => p.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static PickupDelayRuleDefinition ParsePickupRule(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new PickupDelayRuleDefinition();
        try { return JsonSerializer.Deserialize<PickupDelayRuleDefinition>(json) ?? new(); }
        catch { return new PickupDelayRuleDefinition(); }
    }

    private static InTransitDelayRuleDefinition ParseInTransitRule(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new InTransitDelayRuleDefinition();
        try { return JsonSerializer.Deserialize<InTransitDelayRuleDefinition>(json) ?? new(); }
        catch { return new InTransitDelayRuleDefinition(); }
    }

    private static DamageRuleDefinition ParseDamageRule(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new DamageRuleDefinition();
        try { return JsonSerializer.Deserialize<DamageRuleDefinition>(json) ?? new(); }
        catch { return new DamageRuleDefinition(); }
    }
}
