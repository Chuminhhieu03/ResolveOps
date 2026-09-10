using ResolveOps.Domain;

namespace ResolveOps.Application;

public record ErrorDetails(string Message, int HttpStatusCode);

public interface IErrorMessageProvider
{
    Task<ErrorDetails> GetErrorDetailsAsync(DomainError domainError, int defaultStatusCode = 400, CancellationToken cancellationToken = default);
}
