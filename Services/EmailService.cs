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
            _senderEmail = configuration["SendGrid:SenderEmail"]!;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message, byte[]? attachment = null, string? attachmentName = null, string? mimeType = null)
        {
            try
            {
                var msg = new SendGridMessage
                {
                    From = new EmailAddress(_senderEmail),
                    Subject = subject,
                    PlainTextContent = message,
                    HtmlContent = message
                };
                msg.AddTo(new EmailAddress(toEmail));

                if (attachment != null && attachmentName != null && mimeType != null)
                {
                    msg.AddAttachment(attachmentName, Convert.ToBase64String(attachment), mimeType);
                }

                await _sendGridClient.SendEmailAsync(msg);
            }
            catch (Exception ex) {
                throw new Exception(ex.Message);
            }
        }
    }
}

