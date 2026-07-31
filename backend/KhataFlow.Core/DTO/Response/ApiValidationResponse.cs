namespace KhataFlow.Core.DTO.Response;

public record ApiValidationResponse(
    bool Success,
    string Message,
    Dictionary<string, string[]> Errors
);
