namespace SWD392.Service
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using SendGrid;
    using SendGrid.Helpers.Mail;

    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _sendGridApiKey;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _sendGridApiKey = _configuration["SendGrid:ApiKey"];
        }

        public void SendEmail(string recipientEmail, string subject, string plainTextContent, string htmlContent)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress("accountservice@hungngblog.com", "growplus");
            var to = new EmailAddress(recipientEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = client.SendEmailAsync(msg).Result;

            if (response.StatusCode != System.Net.HttpStatusCode.OK && response.StatusCode != System.Net.HttpStatusCode.Accepted)
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

            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            string? baseUrl = _configuration[$"Urls:{environment}"];
            string? confirmationApiPath = _configuration["Urls:ConfirmationApiPath"];
            string confirmationLink = $"{baseUrl}{confirmationApiPath}{jwtToken}";
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
        public void SendPasswordRecoveryEmail(string recipientEmail, string token)
        {
            string subject = "Password Recovery";

            string plainTextContent = $"You can reset your password by clicking the following link: {token}";
            string htmlContent = $@"
                <html>
                    <body>
                        <p>You can reset your password by clicking the link below:</p>
                        <h2>{token}</h2>
                    </body>
                </html>";

            SendEmail(recipientEmail, subject, plainTextContent, htmlContent);
        }
    }
}
