namespace BusTracker.Application.Common.Interfaces
{
    public interface IPhoneNumberService
    {
        bool IsValid(string phoneNumber, string defaultRegion = "IN");

        string? Normalize(string phoneNumber, string defaultRegion = "IN");
    }
}
