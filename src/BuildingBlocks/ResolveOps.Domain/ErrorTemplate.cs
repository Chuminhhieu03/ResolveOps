namespace ResolveOps.Domain;

public sealed class ErrorTemplate : IAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string MessageTemplate { get; private set; } = string.Empty;
    public int HttpStatusCode { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private ErrorTemplate() { }

    public static ErrorTemplate Create(string code, string messageTemplate, int httpStatusCode)
    {
        return new ErrorTemplate
        {
            Code = code.Trim().ToUpperInvariant(),
            MessageTemplate = messageTemplate,
            HttpStatusCode = httpStatusCode
        };
    }

    public void Update(string messageTemplate, int httpStatusCode)
    {
        MessageTemplate = messageTemplate;
        HttpStatusCode = httpStatusCode;
    }
}
