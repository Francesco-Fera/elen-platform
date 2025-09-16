using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Interfaces.Services;

namespace WorkflowEngine.Infrastructure.Services.Email;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _baseUrl;

    // TODO: Implement SendGrid version
    // This would use SendGrid SDK instead of SMTP
    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["Email:SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API Key not configured");
        _fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("FromEmail not configured");
        _fromName = _configuration["Email:FromName"] ?? "WorkflowEngine";
        _baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:3000";
    }

    public async Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationToken)
    {
        throw new NotImplementedException("SendGrid implementation coming soon");
    }

    public async Task<bool> SendPasswordResetAsync(string email, string firstName, string resetToken)
    {
        throw new NotImplementedException("SendGrid implementation coming soon");
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string firstName, string organizationName)
    {
        throw new NotImplementedException("SendGrid implementation coming soon");
    }

    public async Task<bool> SendOrganizationInviteAsync(string email, string inviterName, string organizationName, string inviteToken)
    {
        throw new NotImplementedException("SendGrid implementation coming soon");
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string textBody = null)
    {
        throw new NotImplementedException("SendGrid implementation coming soon");
    }
}