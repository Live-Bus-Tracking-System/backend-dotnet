namespace BusTracker.Application.Common.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public List<string> Errors { get; init; } = [];
        public ApiMeta Meta { get; init; } = new();

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors ?? [] };
    }

    // Non-generic version for responses with no data payload
    public class ApiResponse : ApiResponse<object?>
    {
        public static ApiResponse Ok(string message = "Success") =>
            new() { Success = true, Message = message };

        public static new ApiResponse Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors ?? [] };
    }

    public class ApiMeta
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string? RequestId { get; init; }
    }
}
