using FluentValidation;
using ResolveOps.Domain.Documents;

namespace ResolveOps.Modules.Documents.Features.Evidence.SupersedeDocument;

internal sealed class SupersedeDocumentValidator : AbstractValidator<SupersedeDocumentCommand>
{
    public SupersedeDocumentValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.FileName).NotEmpty().MaximumLength(255);
        RuleFor(c => c.ContentType).NotEmpty();
        RuleFor(c => c.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(EvidenceType.MaxFileSizeBytes);
    }
}
