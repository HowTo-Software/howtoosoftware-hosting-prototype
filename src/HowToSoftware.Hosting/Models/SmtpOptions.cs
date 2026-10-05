using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Hosting.Models;

/// <summary>Operator-managed SMTP delivery; credentials belong in environment configuration.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "HTS Hosting";
    public int DeliveryTimeoutSeconds { get; set; } = 15;

    // A host and sender are both needed. An empty configuration never sends mail or pretends
    // a verification message was delivered. Usernames/passwords are optional for a local relay.
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host)
        && !Host.Contains("REPLACE_ME", StringComparison.OrdinalIgnoreCase)
        && !Host.Contains("_AQUI", StringComparison.OrdinalIgnoreCase)
        && MailAddress.TryCreate(FromAddress, out _);
}

public sealed class SmtpOptionsValidator(IWebHostEnvironment environment) : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.Host.Length > 255
            || (Uri.CheckHostName(options.Host) == UriHostNameType.Unknown && !IPAddress.TryParse(options.Host, out _)))
        {
            failures.Add("Smtp:Host must be a hostname or an IP address, without a URL scheme or port.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Smtp:Port must be between 1 and 65535.");
        }

        if (!options.EnableSsl && !environment.IsDevelopment())
        {
            failures.Add("Smtp:EnableSsl must be true outside Development so verification links travel over TLS.");
        }

        if (!MailAddress.TryCreate(options.FromAddress, out var sender) || sender.DisplayName.Length > 0)
        {
            failures.Add("Smtp:FromAddress must contain one valid sender email address without a display name.");
        }

        if (options.FromName.Length is 0 or > 80 || options.FromName.Any(char.IsControl))
        {
            failures.Add("Smtp:FromName must contain 1-80 characters with no control characters.");
        }

        if (string.IsNullOrWhiteSpace(options.Username) != string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add("Smtp:Username and Smtp:Password must either both be configured or both be omitted.");
        }

        if (options.DeliveryTimeoutSeconds is < 2 or > 60)
        {
            failures.Add("Smtp:DeliveryTimeoutSeconds must be between 2 and 60.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
