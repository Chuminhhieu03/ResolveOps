namespace ResolveOps.Modules.Identity.Features.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);
