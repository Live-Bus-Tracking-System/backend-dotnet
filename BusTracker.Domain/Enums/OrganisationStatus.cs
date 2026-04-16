namespace BusTracker.Domain.Enums
{
    public enum OrganisationStatus
    {
        PendingVerification = 1,  // Registered by client, awaiting SuperAdmin approval
        Active = 2,  // Fully operational
        Suspended = 3,  // Temporarily disabled by SuperAdmin
        Rejected = 4,  // Registration application denied
    }
}
