using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Enums;
using KhataFlow.Core.ServiceContracts;

namespace KhataFlow.Core.Services;

public class CustomerLedgerViewService : ICustomerLedgerViewService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILedgerRepository _ledgerRepository;

    public CustomerLedgerViewService(
        ICustomerRepository customerRepository,
        ILedgerRepository ledgerRepository)
    {
        _customerRepository = customerRepository;
        _ledgerRepository = ledgerRepository;
    }

    private static List<LedgerItemResponse>? BuildItemResponses(LedgerEntry entry)
    {
        if (entry.EntryType != LedgerEntryType.Udhar)
            return null;

        if (entry.Sale?.Items is not { Count: > 0 })
            return null;

        return entry.Sale.Items.Select(i => new LedgerItemResponse
        {
            ProductName = i.Product.ProductName,
            ProductNameUr = i.Product.ProductNameUr,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.Total
        }).ToList();
    }

    public async Task<CustomerLedgerViewResponse?> GetPublicLedgerViewAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var customer = await _customerRepository.GetByPublicTokenAsync(token, ct);
        if (customer is null)
            return null;

        var entries = await _ledgerRepository.GetByCustomerIdAsync(customer.Id, customer.BusinessId, ct);
        var ordered = entries.OrderBy(e => e.Date).ToList();

        decimal running = 0;
        var history = new List<LedgerHistoryItemResponse>(ordered.Count);

        foreach (var entry in ordered)
        {
            running += entry.EntryType == LedgerEntryType.Udhar ? entry.Amount : -entry.Amount;
            history.Add(new LedgerHistoryItemResponse
            {
                Type = entry.EntryType.ToString(),
                Amount = entry.Amount,
                Date = entry.Date,
                Description = entry.Notes,
                RunningBalance = running,
                Items = BuildItemResponses(entry)   // NEW
            });
        }

        return new CustomerLedgerViewResponse
        {
            CustomerName = customer.Name,
            CustomerNameUr = customer.NameUr ?? string.Empty,
            BusinessName = customer.Business?.BusinessName ?? string.Empty,
            BusinessNameUr = customer.Business?.BusinessNameUr ?? string.Empty,
            CurrentBalance = customer.OutstandingBalance,
            History = history.OrderByDescending(h => h.Date).ToList()
        };
    }
}