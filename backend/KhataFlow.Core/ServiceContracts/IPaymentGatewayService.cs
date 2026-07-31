using KhataFlow.Core.DTO.Request;

namespace KhataFlow.Core.ServiceContracts;

public interface IPaymentGatewayService
{
    Task<string> CreateHostedCheckoutAsync(PaymentGatewayRequest request, CancellationToken ct = default);
}

