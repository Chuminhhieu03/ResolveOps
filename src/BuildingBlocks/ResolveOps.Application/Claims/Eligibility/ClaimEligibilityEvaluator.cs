using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Domain.Claims.Eligibility;
using ResolveOps.Domain.Claims;

using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;

namespace ResolveOps.Application.Claims.Eligibility;

public class ClaimEligibilityEvaluator : IClaimEligibilityEvaluator
{
    private readonly IBusinessCalendarService _businessCalendarService;
    private readonly TimeProvider _timeProvider;

    public ClaimEligibilityEvaluator(
        IBusinessCalendarService businessCalendarService,
        TimeProvider timeProvider)
    {
        _businessCalendarService = businessCalendarService;
        _timeProvider = timeProvider;
    }

    public async Task<ClaimEligibilityResult> EvaluateAsync(
        ExceptionCase exceptionCase,
        Guid carrierId,
        string claimType,
        CancellationToken cancellationToken = default)
    {
        var reasonCodes = new List<string>();
        var status = ClaimEligibilityStatus.Eligible;

        if (exceptionCase.Status == ExceptionCaseStatus.Cancelled)
        {
            return new ClaimEligibilityResult(
                ClaimEligibilityStatus.NotEligible,
                ["CASE_CANCELLED"],
                null,
                null);
        }

        // Compatibility check
        if (!IsCompatible(exceptionCase.ExceptionType, claimType))
        {
            return new ClaimEligibilityResult(
                ClaimEligibilityStatus.NotEligible,
                ["INCOMPATIBLE_EXCEPTION_CLAIM_TYPE"],
                null,
                null);
        }

        // In a real application, we'd fetch the active carrier policy and claim filing window.
        // For MVP, we calculate a standard 30-day business deadline based on the case detection date.
        var detectionDate = exceptionCase.DetectedAtUtc;

        var deadlineAtUtc = await _businessCalendarService.CalculateDeadlineAsync(
            exceptionCase.TenantId,
            detectionDate,
            30 * 24 * 60, // 30 days in minutes
            null,
            cancellationToken);

        return new ClaimEligibilityResult(
            status,
            [.. reasonCodes],
            deadlineAtUtc,
            Guid.NewGuid()); // Simulated policy version ID for MVP
    }

    private static bool IsCompatible(string exceptionType, string claimType)
    {
        return (exceptionType, claimType) switch
        {
            (ExceptionType.Damage, ResolveOps.Domain.Claims.ClaimType.CargoDamage) => true,
            (ExceptionType.Damage, ResolveOps.Domain.Claims.ClaimType.TotalLoss) => true,
            (ExceptionType.PartialDelivery, ResolveOps.Domain.Claims.ClaimType.Shortage) => true,
            (ExceptionType.PartialDelivery, ResolveOps.Domain.Claims.ClaimType.TotalLoss) => true,
            (ExceptionType.PickupDelay, ResolveOps.Domain.Claims.ClaimType.Delay) => true,
            (ExceptionType.InTransitDelay, ResolveOps.Domain.Claims.ClaimType.Delay) => true,
            (ExceptionType.Loss, ResolveOps.Domain.Claims.ClaimType.TotalLoss) => true,
            _ => false
        };
    }
}
