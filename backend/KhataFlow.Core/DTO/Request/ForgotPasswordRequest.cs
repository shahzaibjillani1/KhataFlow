namespace KhataFlow.Core.DTO;

public record ForgotPasswordRequest
{
    public string Email { get; set; } =    string.Empty;
}
