namespace ResolveOps.Modules.Notifications.Services;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string SenderEmail { get; set; } = "notifications@resolveops.local";
    public string SenderName { get; set; } = "ResolveOps System";
}
