using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BusTracker.Infrastructure.Services.Messaging
{
    public class SendGridEmailService : IEmailService
    {
        private readonly SendGridSettings _settings;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IOptions<SendGridSettings> options, ILogger<SendGridEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("SendGrid API Key is missing. Email to {To} was simulated: \n{HtmlBody}", to, htmlBody);
                return;
            }

            var client = new SendGridClient(_settings.ApiKey);
            var fromAddress = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(fromAddress, toAddress, subject, plainTextContent: "", htmlContent: htmlBody);

            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email successfully sent to {To} via SendGrid.", to);
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                throw new Exception($"SendGrid failed to send email to {to}. Status: {response.StatusCode}. Details: {body}");
            }
        }
    }
}
