namespace WorkflowEngine.Application.DTOs.Auth;

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public AuthResponse? Data { get; set; }

    public static AuthResult Success(AuthResponse data) => new() { IsSuccess = true, Data = data };
    public static AuthResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}