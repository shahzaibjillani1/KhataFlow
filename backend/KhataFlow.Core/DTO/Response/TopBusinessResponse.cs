namespace KhataFlow.Core.DTO.Response;

public record TopBusinessResponse(
    Guid BusinessId,
    string BusinessName,
    string? BusinessNameUr,
    decimal Revenue,
    string PlanName,
    double PercentageOfTop
);
