using FluentValidation;

namespace ResolveOps.Modules.Tenancy.Features.UpdateTenant;

public sealed class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DefaultTimezone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
    }
}
