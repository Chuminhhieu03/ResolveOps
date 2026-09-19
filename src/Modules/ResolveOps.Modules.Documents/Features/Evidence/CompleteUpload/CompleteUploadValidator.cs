using FluentValidation;

namespace ResolveOps.Modules.Documents.Features.Evidence.CompleteUpload;

internal sealed class CompleteUploadValidator : AbstractValidator<CompleteUploadCommand>
{
    public CompleteUploadValidator()
    {
        RuleFor(c => c.DocumentId)
            .NotEmpty().WithMessage("Document ID is required.");

        RuleFor(c => c.Sha256)
            .NotEmpty().WithMessage("SHA-256 checksum is required.")
            .Length(64).WithMessage("SHA-256 checksum must be exactly 64 hexadecimal characters.")
            .Matches("^[0-9a-fA-F]{64}$").WithMessage("SHA-256 must be a valid hexadecimal string.");
    }
}
