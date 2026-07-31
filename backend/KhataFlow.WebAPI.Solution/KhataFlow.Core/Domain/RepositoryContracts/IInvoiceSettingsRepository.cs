using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface IInvoiceSettingsRepository
{
    Task<InvoiceSettings?> GetByBusinessIdAsync(Guid businessId);
    Task<InvoiceSettings> UpsertAsync(InvoiceSettings settings);
}