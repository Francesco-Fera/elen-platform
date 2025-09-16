namespace WorkflowEngine.Application.Interfaces.Services;

public interface IEmailService
{
    Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationToken);
    Task<bool> SendPasswordResetAsync(string email, string firstName, string resetToken);
    Task<bool> SendWelcomeEmailAsync(string email, string firstName, string organizationName);
    Task<bool> SendOrganizationInviteAsync(string email, string inviterName, string organizationName, string inviteToken);
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string textBody = null);
}