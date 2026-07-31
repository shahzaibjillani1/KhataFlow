using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.ServiceContracts;

public interface IInvoiceDocumentBuilder
{
    byte[] Build(Sale sale, InvoiceSettings settings, Business business);
}