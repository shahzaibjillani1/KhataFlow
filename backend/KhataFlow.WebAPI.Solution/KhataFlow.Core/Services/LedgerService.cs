using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _ledgerRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly IValidator<AddUdharRequest> _addValidator;
    private readonly IValidator<RecordPaymentRequest> _paymentValidator;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LedgerService(
        ILedgerRepository ledgerRepository,
        ICustomerRepository customerRepository,
        INotificationService notificationService,
        IMapper mapper,
        IValidator<AddUdharRequest> addValidator,
        IValidator<RecordPaymentRequest> paymentValidator,
        IStringLocalizer<SharedResource> localizer)
    {
        _ledgerRepository = ledgerRepository;
        _customerRepository = customerRepository;
        _notificationService = notificationService;
        _mapper = mapper;
        _addValidator = addValidator;
        _paymentValidator = paymentValidator;
        _localizer = localizer;
    }

    private static bool IsSettlement(LedgerEntryType type) =>
        type == LedgerEntryType.Cash || type == LedgerEntryType.Card;

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

    public async Task<CustomerKhataResponse> GetKhataAsync(
        Guid businessId, Guid customerId, DateTime? before, int limit)
    {
        var customer = await GetValidatedCustomerAsync(customerId, businessId);

        var result = await _ledgerRepository.GetPagedByCustomerIdAsync(customerId, businessId, before, limit);

        var runningBalance = result.BalanceBeforeBatch;
        var entryResponses = result.Entries.Select(entry =>
        {
            runningBalance += entry.EntryType == LedgerEntryType.Udhar ? entry.Amount : -entry.Amount;

            var response = _mapper.Map<LedgerEntryResponse>(entry);
            response.RunningBalance = runningBalance;
            response.Items = BuildItemResponses(entry);
            return response;
        }).ToList();

        var (totalPurchases, totalPaid) = await _ledgerRepository.GetTotalsAsync(customerId, businessId);

        return new CustomerKhataResponse
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            PhoneNumber = customer.PhoneNumber,
            TotalPurchases = totalPurchases,
            TotalPaid = totalPaid,
            Outstanding = totalPurchases - totalPaid,
            Entries = entryResponses,
            HasMore = result.HasMore,
            NextCursor = result.Entries.Count > 0 ? result.Entries[0].Date : null
        };
    }

    public async Task<LedgerEntryResponse> AddUdharAsync(Guid businessId, AddUdharRequest request)
    {
        await ValidateRequestAsync(_addValidator, request);

        var customer = await GetValidatedCustomerAsync(request.CustomerId, businessId);

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            BusinessId = businessId,
            EntryType = LedgerEntryType.Udhar,
            Amount = request.Amount,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var addedEntry = await _ledgerRepository.AddAsync(entry);

        customer.LastVisit = DateTime.UtcNow;
        await _customerRepository.UpdateAsync(customer);

        var runningBalance = await _ledgerRepository.GetOutstandingBalanceAsync(request.CustomerId, businessId);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: _localizer["Ledger.Notification.UdharRecorded.Title"],
            Message: string.Format(
                _localizer["Ledger.Notification.UdharRecorded.Message"],
                request.Amount, customer.Name, runningBalance),
            Type: NotificationType.UdharReminder,
            BusinessId: businessId,
            ReferenceId: addedEntry.Id));

        var response = _mapper.Map<LedgerEntryResponse>(addedEntry);
        response.RunningBalance = runningBalance;
        response.Items = BuildItemResponses(addedEntry); // always null here — manual entry, no linked Sale

        return response;
    }

    public async Task<LedgerEntryResponse> RecordPaymentAsync(Guid businessId, RecordPaymentRequest request)
    {
        await ValidateRequestAsync(_paymentValidator, request);

        var customer = await GetValidatedCustomerAsync(request.CustomerId, businessId);

        var (totalPurchases, totalPaid) = await _ledgerRepository.GetTotalsAsync(request.CustomerId, businessId);
        var outstanding = totalPurchases - totalPaid;

        if (outstanding <= 0)
            throw new DomainException(
                _localizer["Ledger.NoOutstandingBalance"]);

        if (request.Amount > outstanding)
            throw new DomainException(
                string.Format(_localizer["Ledger.PaymentExceedsOutstanding"], request.Amount, outstanding));

        var paymentEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            BusinessId = businessId,
            EntryType = LedgerEntryType.Cash,
            Amount = request.Amount,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var addedPayment = await _ledgerRepository.AddAsync(paymentEntry);

        var runningBalance = await _ledgerRepository.GetOutstandingBalanceAsync(request.CustomerId, businessId);

        await TryNotifyAsync(new CreateNotificationRequest(
            Target: NotificationTarget.Business,
            Title: _localizer["Ledger.Notification.PaymentReceived.Title"],
            Message: string.Format(
                _localizer["Ledger.Notification.PaymentReceived.Message"],
                request.Amount, customer.Name, runningBalance),
            Type: NotificationType.PaymentReceived,
            BusinessId: businessId,
            ReferenceId: addedPayment.Id));

        var response = _mapper.Map<LedgerEntryResponse>(addedPayment);
        response.RunningBalance = runningBalance;
        response.Items = BuildItemResponses(addedPayment); 

        return response;
    }

    private async Task TryNotifyAsync(CreateNotificationRequest request)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(request);
        }
        catch
        {
        }
    }

    private async Task ValidateRequestAsync<T>(
        IValidator<T> validator,
        T request)
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
    }

    private async Task<Customer> GetValidatedCustomerAsync(
        Guid customerId,
        Guid businessId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException(_localizer["Ledger.CustomerNotFound"]);

        if (customer.BusinessId != businessId)
            throw new DomainException(
                _localizer["Ledger.CustomerBusinessMismatch"]);

        return customer;
    }
}