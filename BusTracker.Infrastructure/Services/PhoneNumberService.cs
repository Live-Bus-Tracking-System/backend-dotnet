using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using PhoneNumbers;

namespace BusTracker.Infrastructure.Services
{
    public class PhoneNumberService : IPhoneNumberService
    {
        private static readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();

        public bool IsValid(string phoneNumber, string defaultRegion = "IN")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            try
            {
                var parsedNumber = _phoneUtil.Parse(phoneNumber, defaultRegion);
                return _phoneUtil.IsValidNumber(parsedNumber);
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        public string? Normalize(string phoneNumber, string defaultRegion = "IN")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            try
            {
                var parsedNumber = _phoneUtil.Parse(phoneNumber, defaultRegion);
                if (!_phoneUtil.IsValidNumber(parsedNumber))
                {
                    return null;
                }

                return _phoneUtil.Format(parsedNumber, PhoneNumberFormat.E164);
            }
            catch (NumberParseException)
            {
                return null;
            }
        }
    }
}
