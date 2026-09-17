namespace ResolveOps.Modules.Exceptions.Services;

public interface IExceptionFingerprintGenerator
{
    string Generate(
        Guid tenantId,
        Guid shipmentId,
        Guid? shipmentLegId,
        string exceptionType,
        string businessKey,
        int policyVersion);
}

public sealed class ExceptionFingerprintGenerator : IExceptionFingerprintGenerator
{
    public string Generate(
        Guid tenantId,
        Guid shipmentId,
        Guid? shipmentLegId,
        string exceptionType,
        string businessKey,
        int policyVersion)
    {
        var legPart = shipmentLegId.HasValue ? shipmentLegId.Value.ToString("D") : "NONE";
        var raw = $"{tenantId:D}:{shipmentId:D}:{legPart}:{exceptionType.Trim()}:{businessKey.Trim()}:v{policyVersion}";

        if (raw.Length > 300)
        {
            // Truncate business key if needed to fit database constraint
            var prefix = $"{tenantId:D}:{shipmentId:D}:{legPart}:{exceptionType.Trim()}:";
            var suffix = $":v{policyVersion}";
            var allowedBusinessKeyLength = 300 - prefix.Length - suffix.Length;
            if (allowedBusinessKeyLength > 0 && businessKey.Length > allowedBusinessKeyLength)
            {
                var truncatedKey = businessKey[..allowedBusinessKeyLength];
                return $"{prefix}{truncatedKey}{suffix}";
            }
        }

        return raw;
    }
}
