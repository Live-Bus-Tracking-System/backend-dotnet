using MediatR;

namespace BusTracker.Application.Features.Auth.Events
{
    public record LoginEvent(string UserId, string IpAddress) : INotification;
    
    public record RegisterEvent(string UserId, string FullName, string? Email, string PhoneNumber) : INotification;
    
    public record ChangePasswordEvent(string UserId) : INotification;
    
    public record ForgotPasswordEvent(string EmailOrPhone, string ResetToken) : INotification;
    
    public record ResetPasswordEvent(string EmailOrPhone) : INotification;
}
