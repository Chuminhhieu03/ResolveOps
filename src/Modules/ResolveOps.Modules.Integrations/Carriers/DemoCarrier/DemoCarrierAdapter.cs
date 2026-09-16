using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ResolveOps.Domain.Tracking;

namespace ResolveOps.Modules.Integrations.Carriers.DemoCarrier;

/// <summary>
/// Intermediate representation of a parsed carrier tracking event before shipment matching.
/// </summary>
public sealed record NormalizedCarrierEvent(
    string ExternalEventId,
    string TrackingNumber,
    string EventType,
    string? EventCode,
    DateTimeOffset OccurredAtUtc,
    string? LocationText,
    int? PackageCount,
    decimal? Quantity,
    string? Notes);

/// <summary>
/// Raw DTO representing the incoming JSON payload from DemoCarrier webhook.
/// </summary>
public sealed record DemoCarrierPayload(
    string? EventId,
    string? TrackingNumber,
    string? StatusCode,
    string? StatusDescription,
    DateTimeOffset? Timestamp,
    string? Location,
    int? PackageCount,
    decimal? Weight,
    string? WeightUnit,
    string? ExceptionReason);

/// <summary>
/// Carrier adapter for DemoCarrier (spec §24 Phase 6 task 2).
/// Validates HMAC-SHA256 signatures, parses payload JSON, and normalizes into canonical tracking events.
/// </summary>
public static class DemoCarrierAdapter
{
    public const string CarrierCode = "DEMOCARRIER";

    // Default shared secret for local and automated testing
    public const string DefaultSecretKey = "democarrier-secret-key-12345";

    /// <summary>
    /// Validates HMAC-SHA256 signature using constant-time comparison (spec §19.5, §24 Phase 6 task 3).
    /// </summary>
    public static bool ValidateSignature(byte[] bodyBytes, string? providedSignature, string secretKey = DefaultSecretKey)
    {
        if (string.IsNullOrWhiteSpace(providedSignature))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var computedHash = hmac.ComputeHash(bodyBytes);
        var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();

        var normalizedProvided = providedSignature.Trim().ToLowerInvariant();

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(normalizedProvided));
    }

    /// <summary>
    /// Computes the HMAC-SHA256 hex signature for a payload (used by fixtures/test simulator).
    /// </summary>
    public static string ComputeSignature(string jsonPayload, string secretKey = DefaultSecretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(jsonPayload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Parses raw JSON into a NormalizedCarrierEvent.
    /// Returns null if payload is invalid or missing required fields.
    /// </summary>
    public static NormalizedCarrierEvent? Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        DemoCarrierPayload? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DemoCarrierPayload>(rawJson, _serializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto is null ||
            string.IsNullOrWhiteSpace(dto.EventId) ||
            string.IsNullOrWhiteSpace(dto.TrackingNumber) ||
            dto.Timestamp is null)
        {
            return null;
        }

        var eventType = MapStatusCode(dto.StatusCode, dto.ExceptionReason);

        return new NormalizedCarrierEvent(
            ExternalEventId: dto.EventId.Trim(),
            TrackingNumber: dto.TrackingNumber.Trim(),
            EventType: eventType,
            EventCode: dto.StatusCode?.Trim(),
            OccurredAtUtc: dto.Timestamp.Value,
            LocationText: dto.Location?.Trim(),
            PackageCount: dto.PackageCount,
            Quantity: dto.Weight,
            Notes: string.IsNullOrWhiteSpace(dto.ExceptionReason) ? dto.StatusDescription : dto.ExceptionReason);
    }

    private static string MapStatusCode(string? statusCode, string? exceptionReason)
    {
        if (!string.IsNullOrWhiteSpace(exceptionReason))
        {
            return TrackingEventType.Damaged;
        }

        return (statusCode?.Trim().ToUpperInvariant()) switch
        {
            "PU" => TrackingEventType.PickedUp,
            "IT" => TrackingEventType.InTransit,
            "OFD" => TrackingEventType.OutForDelivery,
            "DEL" => TrackingEventType.Delivered,
            "DMG" => TrackingEventType.Damaged,
            "DLY" => TrackingEventType.Delay,
            "EXC" => TrackingEventType.Exception,
            _ => TrackingEventType.InTransit,
        };
    }
}
