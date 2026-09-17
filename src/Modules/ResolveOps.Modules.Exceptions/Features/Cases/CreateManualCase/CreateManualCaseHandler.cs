using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Messaging;
using ResolveOps.Messaging.Events;
using ResolveOps.Modules.Exceptions.Services;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CreateManualCase;

internal sealed class CreateManualCaseHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly IExceptionFingerprintGenerator _fingerprintGenerator;
    private readonly ISeverityCalculator _severityCalculator;
    private readonly IAssignmentEvaluator _assignmentEvaluator;
    private readonly IOutboxWriter _outboxWriter;

    public CreateManualCaseHandler(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        IExceptionFingerprintGenerator fingerprintGenerator,
        ISeverityCalculator severityCalculator,
        IAssignmentEvaluator assignmentEvaluator,
        IOutboxWriter outboxWriter)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
        _fingerprintGenerator = fingerprintGenerator;
        _severityCalculator = severityCalculator;
        _assignmentEvaluator = assignmentEvaluator;
        _outboxWriter = outboxWriter;
    }

    public async Task<Result<CreateManualCaseResponse>> HandleAsync(
        CreateManualCaseCommand command,
        Guid? currentUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;
        var now = _timeProvider.GetUtcNow();

        // 1. Verify shipment exists for this tenant
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(s => s.Id == command.ShipmentId, cancellationToken);

        if (shipment is null)
        {
            return DomainError.Failure("ERR_SHIPMENT_NOT_FOUND", $"Shipment with ID '{command.ShipmentId}' was not found.");
        }

        // 2. Resolve active policy for this exception type
        var policy = await _dbContext.ExceptionPolicies
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId &&
                        p.ExceptionType == command.ExceptionType &&
                        p.Status == PolicyStatus.Active)
            .OrderByDescending(p => p.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var policyVersion = policy?.VersionNumber ?? 1;
        var policyId = policy?.Id ?? Guid.Empty;

        // 3. Calculate severity and team
        var exposure = command.FinancialExposure ?? shipment.DeclaredValue ?? 0m;
        var currency = command.ExposureCurrency ?? shipment.DeclaredValueCurrency ?? "USD";

        var severityCalc = _severityCalculator.Calculate(
            policy?.SeverityDefinitionJson,
            exposure,
            delayMinutes: null,
            initialSeverityHint: command.Severity);

        var severity = !string.IsNullOrWhiteSpace(command.Severity) ? command.Severity : severityCalc.Severity;
        var ownerTeam = !string.IsNullOrWhiteSpace(command.OwnerTeamCode)
            ? command.OwnerTeamCode
            : _assignmentEvaluator.SelectOwnerTeam(policy?.AssignmentDefinitionJson, severity, command.ExceptionType);

        // 4. Generate deterministic fingerprint
        var businessKey = $"MANUAL-{Guid.NewGuid():N}"[..18];
        var fingerprint = _fingerprintGenerator.Generate(
            tenantId,
            shipment.Id,
            command.ShipmentLegId,
            command.ExceptionType,
            businessKey,
            policyVersion);

        // 5. Generate case number: EXC-YYYY-NNNNNN
        var caseNumber = $"EXC-{now.Year}-{RandomNumberGenerator.GetInt32(100000, 999999)}";

        // 6. Create aggregate root
        var exceptionCase = ExceptionCase.Create(
            tenantId,
            caseNumber,
            shipment.Id,
            command.ShipmentLegId,
            command.ExceptionType,
            fingerprint,
            severity,
            severityCalc.SeverityScore,
            policyId,
            policyVersion,
            ownerTeam,
            exposure,
            currency,
            detectedAtUtc: now,
            timeProvider: _timeProvider,
            initialSummary: $"Manually created: {command.Reason}",
            correlationId: correlationId,
            actorId: currentUserId,
            actorType: ActorType.User);

        _dbContext.ExceptionCases.Add(exceptionCase);

        // 7. Write integration event atomically to outbox
        var detectedEvent = new ExceptionDetectedV1
        {
            CaseId = exceptionCase.Id,
            CaseNumber = exceptionCase.CaseNumber,
            ShipmentId = exceptionCase.ShipmentId,
            ExceptionType = exceptionCase.ExceptionType,
            Severity = exceptionCase.Severity,
            DetectedAtUtc = exceptionCase.DetectedAtUtc,
            OwnerTeamCode = exceptionCase.OwnerTeamCode
        };

        _outboxWriter.Write(detectedEvent, tenantId, correlationId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateManualCaseResponse>.Success(new CreateManualCaseResponse(
            exceptionCase.Id,
            exceptionCase.CaseNumber,
            exceptionCase.ShipmentId,
            exceptionCase.ExceptionType,
            exceptionCase.Status,
            exceptionCase.Severity,
            exceptionCase.OwnerTeamCode,
            exceptionCase.FinancialExposure,
            exceptionCase.ExposureCurrency,
            exceptionCase.DetectedAtUtc,
            exceptionCase.ConcurrencyStamp));
    }
}
