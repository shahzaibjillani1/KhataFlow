namespace KhataFlow.Core.ServiceContracts;

public interface IInvoiceService
{
    Task<byte[]> GenerateInvoicePdfAsync(Guid businessId, Guid saleId);
}