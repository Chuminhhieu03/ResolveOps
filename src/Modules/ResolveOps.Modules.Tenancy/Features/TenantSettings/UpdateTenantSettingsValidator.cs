using System.Text.Json;
using FluentValidation;

namespace ResolveOps.Modules.Tenancy.Features.TenantSettings;

public sealed class UpdateTenantSettingsValidator : AbstractValidator<UpdateTenantSettingsCommand>
{
    public UpdateTenantSettingsValidator()
    {
        RuleFor(x => x.SettingsJson)
            .NotEmpty()
            .Must(BeValidJson).WithMessage("SettingsJson must be a valid JSON object.");
    }

    private bool BeValidJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
