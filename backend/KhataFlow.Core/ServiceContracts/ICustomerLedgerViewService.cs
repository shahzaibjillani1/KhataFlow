using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ICustomerLedgerViewService
{
    Task<CustomerLedgerViewResponse?> GetPublicLedgerViewAsync(string token, CancellationToken ct = default);
}