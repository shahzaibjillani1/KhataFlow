using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

public interface ILedgerService
{
    Task<CustomerKhataResponse> GetKhataAsync(Guid businessId, Guid customerId, DateTime? before, int limit);
    Task<LedgerEntryResponse> AddUdharAsync(Guid businessId, AddUdharRequest request);
    Task<LedgerEntryResponse> RecordPaymentAsync(Guid businessId, RecordPaymentRequest request);
}