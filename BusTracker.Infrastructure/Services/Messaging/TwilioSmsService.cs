using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BusTracker.Infrastructure.Services.Messaging
{
    public class TwilioSmsService : ISmsService
    {
        private readonly TwilioSettings _settings;
        private readonly ILogger<TwilioSmsService> _logger;

        public TwilioSmsService(IOptions<TwilioSettings> options, ILogger<TwilioSmsService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendSmsAsync(string toPhoneNumber, string messageBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.AccountSid) || string.IsNullOrWhiteSpace(_settings.AuthToken))
            {
                _logger.LogWarning("Twilio API Key is missing. SMS to {To} was simulated: {Message}", toPhoneNumber, messageBody);
                return;
            }

            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);

            var messageOptions = new CreateMessageOptions(new PhoneNumber(toPhoneNumber))
            {
                From = new PhoneNumber(_settings.FromPhoneNumber),
                Body = messageBody
            };

            var response = await MessageResource.CreateAsync(messageOptions);

            _logger.LogInformation("SMS sent to {To} with Twilio SID: {Sid}", toPhoneNumber, response.Sid);
        }
    }
}
