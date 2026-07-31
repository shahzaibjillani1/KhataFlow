using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface IInvoiceSettingsService
{
    Task<InvoiceSettingsResponse> GetAsync(Guid businessId);
    Task<InvoiceSettingsResponse> UpdateAsync(InvoiceSettingsRequest request, Guid businessId);
}