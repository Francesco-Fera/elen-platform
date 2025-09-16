using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using WorkflowEngine.Application.DTOs.Email;
using WorkflowEngine.Application.Interfaces.Services;

namespace WorkflowEngine.Infrastructure.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;
    private readonly string _baseUrl;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Email configuration from appsettings.json
        _smtpHost = _configuration["Email:Smtp:Host"] ?? throw new InvalidOperationException("Email:Smtp:Host not configured");
        _smtpPort = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
        _smtpUsername = _configuration["Email:Smtp:Username"] ?? throw new InvalidOperationException("Email:Smtp:Username not configured");
        _smtpPassword = _configuration["Email:Smtp:Password"] ?? throw new InvalidOperationException("Email:Smtp:Password not configured");
        _fromEmail = _configuration["Email:FromEmail"] ?? _smtpUsername;
        _fromName = _configuration["Email:FromName"] ?? "WorkflowEngine";
        _enableSsl = bool.Parse(_configuration["Email:Smtp:EnableSsl"] ?? "true");
        _baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:3000";
    }

    public async Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationToken)
    {
        try
        {
            var verificationLink = $"{_baseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
            var template = EmailTemplates.EmailVerification(firstName, verificationLink);

            _logger.LogInformation("Sending email verification to {Email}", email);

            return await SendEmailAsync(email, template.Subject, template.HtmlBody, template.TextBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetAsync(string email, string firstName, string resetToken)
    {
        try
        {
            var resetLink = $"{_baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
            var template = EmailTemplates.PasswordReset(firstName, resetLink);

            _logger.LogInformation("Sending password reset to {Email}", email);

            return await SendEmailAsync(email, template.Subject, template.HtmlBody, template.TextBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string firstName, string organizationName)
    {
        try
        {
            var template = EmailTemplates.Welcome(firstName, organizationName);

            _logger.LogInformation("Sending welcome email to {Email}", email);

            return await SendEmailAsync(email, template.Subject, template.HtmlBody, template.TextBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendOrganizationInviteAsync(string email, string inviterName, string organizationName, string inviteToken)
    {
        try
        {
            var inviteLink = $"{_baseUrl}/invite?token={Uri.EscapeDataString(inviteToken)}";

            var template = new EmailTemplate
            {
                Subject = $"{inviterName} invited you to join {organizationName} on WorkflowEngine",
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Organization Invitation</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 40px 20px;'>
        <div style='text-align: center; margin-bottom: 40px;'>
            <div style='width: 64px; height: 64px; background-color: #8b5cf6; border-radius: 12px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;'>
                <svg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2'>
                    <path d='M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2'/>
                    <circle cx='8.5' cy='7' r='4'/>
                    <path d='M20 8v6M23 11h-6'/>
                </svg>
            </div>
            <h1 style='color: #1f2937; margin: 0; font-size: 24px; font-weight: bold;'>You're Invited!</h1>
        </div>

        <div style='margin-bottom: 40px;'>
            <p style='color: #6b7280; line-height: 1.6; margin-bottom: 24px;'>
                <strong>{inviterName}</strong> has invited you to join <strong>{organizationName}</strong> on WorkflowEngine.
            </p>
            
            <div style='text-align: center; margin: 32px 0;'>
                <a href='{inviteLink}' 
                   style='background-color: #8b5cf6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; display: inline-block;'>
                    Accept Invitation
                </a>
            </div>
            
            <p style='color: #6b7280; line-height: 1.6; font-size: 14px;'>
                If the button doesn't work, copy and paste this link:<br>
                <a href='{inviteLink}' style='color: #8b5cf6; word-break: break-all;'>{inviteLink}</a>
            </p>
        </div>

        <div style='border-top: 1px solid #e5e7eb; padding-top: 24px; text-align: center;'>
            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>
                This invitation will expire in 7 days.
            </p>
        </div>
    </div>
</body>
</html>",
                TextBody = $@"
You're Invited!

{inviterName} has invited you to join {organizationName} on WorkflowEngine.

Accept the invitation by clicking this link:
{inviteLink}

This invitation will expire in 7 days.

Best regards,
The WorkflowEngine Team
"
            };

            _logger.LogInformation("Sending organization invite to {Email}", email);

            return await SendEmailAsync(email, template.Subject, template.HtmlBody, template.TextBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send organization invite to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string textBody = null)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort);
            client.EnableSsl = _enableSsl;
            client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            client.Timeout = 30000; // 30 seconds timeout

            using var message = new MailMessage();
            message.From = new MailAddress(_fromEmail, _fromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.IsBodyHtml = true;

            // Create multipart message with HTML and text versions
            if (!string.IsNullOrEmpty(htmlBody))
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                message.AlternateViews.Add(htmlView);
            }

            if (!string.IsNullOrEmpty(textBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                message.AlternateViews.Add(textView);
            }

            // Fallback body
            message.Body = htmlBody ?? textBody ?? "";

            await client.SendMailAsync(message);

            _logger.LogInformation("Email sent successfully to {Email}", to);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Email}: {Error}", to, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Email}", to);
            return false;
        }
    }
}