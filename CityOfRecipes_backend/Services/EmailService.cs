using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace CityOfRecipes_backend.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;

        public EmailService(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string fromEmail)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUser = smtpUser;
            _smtpPass = smtpPass;
            _fromEmail = fromEmail;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Створюємо лист
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_fromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            // Налаштовуємо SMTP-клієнт
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_smtpUser, _smtpPass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
