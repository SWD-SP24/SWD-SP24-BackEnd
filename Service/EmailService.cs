namespace SWD392.Service
{
    using System;
    using System.Collections.Generic;
    using Azure.Communication.Email;
    using Azure;
    using System.Net.Mail;

    public class EmailService
    {
        private readonly EmailClient _emailClient;

        public EmailService(string connectionString)
        {
            _emailClient = new EmailClient(connectionString);
        }

        public void SendEmail(string recipientEmail, string subject, string plainTextContent, string htmlContent)
        {
            var emailMessage = new EmailMessage(
                senderAddress: "DoNotReply@hungngblog.com",
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                new(recipientEmail)
                }));

            EmailSendOperation emailSendOperation = _emailClient.Send(
                WaitUntil.Completed,
                emailMessage);

            if (emailSendOperation.HasCompleted)
            {
                Console.WriteLine("Email sent successfully.");
            }
            else
            {
                throw new Exception("Failed to send email.");
            }
        }

        /// <summary>
        /// Sends an account confirmation email with a JWT token.
        /// </summary>
        public void SendAccountConfirmationEmail(string recipientEmail, string jwtToken)
        {
            string subject = "Account Confirmation";
            string confirmationLink = $"https://yourwebsite.com/confirm-account?token={jwtToken}";
            string plainTextContent = $"Please confirm your account by clicking the following link: {confirmationLink}";
            string htmlContent = $@"
                <html>
                    <body>
                        <p>Please confirm your account by clicking the link below:</p>
                        <a href='{confirmationLink}'>Confirm Account</a>
                    </body>
                </html>";

            SendEmail(recipientEmail, subject, plainTextContent, htmlContent);
        }

        /// <summary>
        /// Sends a password recovery email with a JWT token.
        /// </summary>
        public void SendPasswordRecoveryEmail(string recipientEmail, string jwtToken)
        {
            string subject = "Password Recovery";
            string recoveryLink = $"https://yourwebsite.com/reset-password?token={jwtToken}";
            string plainTextContent = $"You can reset your password by clicking the following link: {recoveryLink}";
            string htmlContent = $@"
                <html>
                    <body>
                        <p>You can reset your password by clicking the link below:</p>
                        <a href='{recoveryLink}'>Reset Password</a>
                    </body>
                </html>";

            SendEmail(recipientEmail, subject, plainTextContent, htmlContent);
        }
    }
}
