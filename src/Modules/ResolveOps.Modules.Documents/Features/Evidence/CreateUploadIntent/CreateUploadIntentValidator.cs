using FluentValidation;
using ResolveOps.Domain.Documents;

namespace ResolveOps.Modules.Documents.Features.Evidence.CreateUploadIntent;

internal sealed class CreateUploadIntentValidator : AbstractValidator<CreateUploadIntentCommand>
{
    public CreateUploadIntentValidator()
    {
        RuleFor(c => c.CaseId)
            .NotEmpty().WithMessage("Case ID is required.");

        RuleFor(c => c.EvidenceType)
            .NotEmpty().WithMessage("Evidence type is required.")
            .Must(EvidenceType.IsValid)
            .WithMessage(c => $"'{c.EvidenceType}' is not a valid evidence type.");

        RuleFor(c => c.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(255).WithMessage("File name must not exceed 255 characters.");

        RuleFor(c => c.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must((cmd, ct) => EvidenceType.IsMimeTypeAllowed(cmd.EvidenceType, ct))
            .When(c => EvidenceType.IsValid(c.EvidenceType))
            .WithMessage(c => $"Content type '{c.ContentType}' is not allowed for evidence type '{c.EvidenceType}'.");

        RuleFor(c => c.SizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than zero.")
            .LessThanOrEqualTo(EvidenceType.MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {EvidenceType.MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(c => c.Issuer)
            .MaximumLength(250).When(c => c.Issuer is not null)
            .WithMessage("Issuer must not exceed 250 characters.");
    }
}
