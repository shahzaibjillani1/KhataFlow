namespace KhataFlow.Core.DTO.Request;

public record PaymentGatewayRequest(
    decimal Amount,
    string BasketId,
    string CustomerMobile,
    string CustomerEmail,
    string SuccessUrl,
    string FailureUrl,
    string? Description = null
);
