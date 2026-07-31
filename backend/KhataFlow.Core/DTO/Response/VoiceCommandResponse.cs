using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Response;

public class VoiceCommandResponse
{
    public VoiceIntent Intent { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; } 
    public string? ErrorMessage { get; set; }
}