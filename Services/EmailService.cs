using SendGrid.Helpers.Mail;
using SendGrid;

namespace AvanzarBackEnd.Services
{
    public class EmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly string _senderEmail;

        public EmailService(ISendGridClient sendGridClient, IConfiguration configuration)
        {
            _sendGridClient = sendGridClient;
            _senderEmail = configuration["SendGrid:SenderEmail"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_senderEmail),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            await _sendGridClient.SendEmailAsync(msg);
        }
    }
}
