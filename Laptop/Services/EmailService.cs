using MailKit.Net.Smtp;
using MimeKit;

namespace LaptopShop.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient()) 
            {
                try
                {
                    await client.ConnectAsync(emailSettings["SMTPServer"], int.Parse(emailSettings["Port"]), false);
                    await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["Password"]);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex) {
                    throw new InvalidOperationException($"Error sending email: {ex.Message}", ex);
                }
            }

        }

    }
}
