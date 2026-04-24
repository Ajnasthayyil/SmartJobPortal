namespace SmartJobPortal.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public static ApiResponse<T> Fail(string message, int statusCode = 400) =>
        new() { Success = false, Message = message, StatusCode = statusCode };

    public static ApiResponse<T> NotFound(string message = "Not found") =>
        Fail(message, 404);

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized") =>
        Fail(message, 401);

    // ✅ Matches usage in AuthService
    public static ApiResponse<T> SuccessResponse(T? data, string message = "Success") =>
        Ok(data!, message);

    public static ApiResponse<T> FailureResponse(List<string> errors, string message = "Failed", int statusCode = 400) =>
        new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
}