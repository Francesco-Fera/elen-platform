namespace WorkflowEngine.Application.DTOs.Email;

public class EmailTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
}

public static class EmailTemplates
{
    public static EmailTemplate EmailVerification(string firstName, string verificationLink)
    {
        return new EmailTemplate
        {
            Subject = "Verify Your Email Address - WorkflowEngine",
            HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Verification</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 40px 20px;'>
        <!-- Header -->
        <div style='text-align: center; margin-bottom: 40px;'>
            <div style='width: 64px; height: 64px; background-color: #3b82f6; border-radius: 12px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;'>
                <svg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2'>
                    <path d='M13 10V3L4 14h7v7l9-11h-7z'/>
                </svg>
            </div>
            <h1 style='color: #1f2937; margin: 0; font-size: 24px; font-weight: bold;'>WorkflowEngine</h1>
        </div>

        <!-- Main Content -->
        <div style='margin-bottom: 40px;'>
            <h2 style='color: #1f2937; font-size: 20px; margin-bottom: 16px;'>Hi {firstName}!</h2>
            <p style='color: #6b7280; line-height: 1.6; margin-bottom: 24px;'>
                Welcome to WorkflowEngine! Please verify your email address to complete your account setup and start building amazing workflows.
            </p>
            
            <div style='text-align: center; margin: 32px 0;'>
                <a href='{verificationLink}' 
                   style='background-color: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; display: inline-block;'>
                    Verify Email Address
                </a>
            </div>
            
            <p style='color: #6b7280; line-height: 1.6; font-size: 14px; margin-top: 24px;'>
                If the button doesn't work, you can copy and paste this link into your browser:<br>
                <a href='{verificationLink}' style='color: #3b82f6; word-break: break-all;'>{verificationLink}</a>
            </p>
        </div>

        <!-- Footer -->
        <div style='border-top: 1px solid #e5e7eb; padding-top: 24px; text-align: center;'>
            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>
                This verification link will expire in 24 hours for security reasons.
            </p>
            <p style='color: #9ca3af; font-size: 12px; margin: 8px 0 0 0;'>
                If you didn't create an account with WorkflowEngine, you can safely ignore this email.
            </p>
        </div>
    </div>
</body>
</html>",
            TextBody = $@"
Hi {firstName}!

Welcome to WorkflowEngine! Please verify your email address to complete your account setup.

Verify your email by clicking this link:
{verificationLink}

This verification link will expire in 24 hours for security reasons.

If you didn't create an account with WorkflowEngine, you can safely ignore this email.

Best regards,
The WorkflowEngine Team
"
        };
    }

    public static EmailTemplate PasswordReset(string firstName, string resetLink)
    {
        return new EmailTemplate
        {
            Subject = "Reset Your Password - WorkflowEngine",
            HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 40px 20px;'>
        <!-- Header -->
        <div style='text-align: center; margin-bottom: 40px;'>
            <div style='width: 64px; height: 64px; background-color: #ef4444; border-radius: 12px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;'>
                <svg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2'>
                    <path d='M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'/>
                </svg>
            </div>
            <h1 style='color: #1f2937; margin: 0; font-size: 24px; font-weight: bold;'>WorkflowEngine</h1>
        </div>

        <!-- Main Content -->
        <div style='margin-bottom: 40px;'>
            <h2 style='color: #1f2937; font-size: 20px; margin-bottom: 16px;'>Password Reset Request</h2>
            <p style='color: #6b7280; line-height: 1.6; margin-bottom: 24px;'>
                Hi {firstName}, we received a request to reset your password for your WorkflowEngine account.
            </p>
            
            <div style='text-align: center; margin: 32px 0;'>
                <a href='{resetLink}' 
                   style='background-color: #ef4444; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; display: inline-block;'>
                    Reset Password
                </a>
            </div>
            
            <p style='color: #6b7280; line-height: 1.6; font-size: 14px; margin-top: 24px;'>
                If the button doesn't work, you can copy and paste this link into your browser:<br>
                <a href='{resetLink}' style='color: #ef4444; word-break: break-all;'>{resetLink}</a>
            </p>
        </div>

        <!-- Security Notice -->
        <div style='background-color: #fef3c7; border: 1px solid #f59e0b; border-radius: 6px; padding: 16px; margin-bottom: 24px;'>
            <p style='color: #92400e; font-size: 14px; margin: 0; font-weight: 500;'>🔒 Security Notice</p>
            <p style='color: #92400e; font-size: 13px; margin: 8px 0 0 0; line-height: 1.4;'>
                If you didn't request this password reset, please ignore this email. Your account is still secure.
            </p>
        </div>

        <!-- Footer -->
        <div style='border-top: 1px solid #e5e7eb; padding-top: 24px; text-align: center;'>
            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>
                This reset link will expire in 1 hour for security reasons.
            </p>
        </div>
    </div>
</body>
</html>",
            TextBody = $@"
Password Reset Request

Hi {firstName}, we received a request to reset your password for your WorkflowEngine account.

Reset your password by clicking this link:
{resetLink}

🔒 SECURITY NOTICE:
If you didn't request this password reset, please ignore this email. Your account is still secure.

This reset link will expire in 1 hour for security reasons.

Best regards,
The WorkflowEngine Team
"
        };
    }

    public static EmailTemplate Welcome(string firstName, string organizationName)
    {
        return new EmailTemplate
        {
            Subject = "Welcome to WorkflowEngine! 🎉",
            HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to WorkflowEngine</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 40px 20px;'>
        <!-- Header -->
        <div style='text-align: center; margin-bottom: 40px;'>
            <div style='width: 64px; height: 64px; background-color: #10b981; border-radius: 12px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;'>
                <svg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2'>
                    <path d='M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'/>
                </svg>
            </div>
            <h1 style='color: #1f2937; margin: 0; font-size: 24px; font-weight: bold;'>Welcome to WorkflowEngine! 🎉</h1>
        </div>

        <!-- Main Content -->
        <div style='margin-bottom: 40px;'>
            <h2 style='color: #1f2937; font-size: 20px; margin-bottom: 16px;'>Hi {firstName}!</h2>
            <p style='color: #6b7280; line-height: 1.6; margin-bottom: 24px;'>
                Congratulations! Your account has been successfully verified and <strong>{organizationName}</strong> is ready for action.
            </p>
            
            <div style='background-color: #f3f4f6; border-radius: 8px; padding: 24px; margin: 24px 0;'>
                <h3 style='color: #1f2937; font-size: 16px; margin-bottom: 16px;'>🚀 What's next?</h3>
                <ul style='color: #6b7280; line-height: 1.6; padding-left: 20px;'>
                    <li style='margin-bottom: 8px;'>Create your first workflow</li>
                    <li style='margin-bottom: 8px;'>Explore our node library</li>
                    <li style='margin-bottom: 8px;'>Invite team members to collaborate</li>
                    <li style='margin-bottom: 8px;'>Check out workflow templates</li>
                </ul>
            </div>
        </div>

        <!-- Footer -->
        <div style='border-top: 1px solid #e5e7eb; padding-top: 24px; text-align: center;'>
            <p style='color: #6b7280; font-size: 14px; margin: 0 0 16px 0;'>
                Need help getting started? Check out our documentation or contact support.
            </p>
            <p style='color: #9ca3af; font-size: 12px; margin: 0;'>
                Happy automating!<br>
                The WorkflowEngine Team
            </p>
        </div>
    </div>
</body>
</html>",
            TextBody = $@"
Welcome to WorkflowEngine! 🎉

Hi {firstName}!

Congratulations! Your account has been successfully verified and {organizationName} is ready for action.

🚀 What's next?
• Create your first workflow
• Explore our node library  
• Invite team members to collaborate
• Check out workflow templates

Need help getting started? Check out our documentation or contact support.

Happy automating!
The WorkflowEngine Team
"
        };
    }
}