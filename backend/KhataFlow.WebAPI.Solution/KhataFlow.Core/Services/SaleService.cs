using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly INotificationService _notificationService;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IPlanLimitService _planLimitService;
    private readonly IMapper _mapper;
    private readonly IValidator<SaleAddRequest> _addValidator;
    private readonly IValidator<SaleUpdateRequest> _updateValidator;

    private const int LowStockThreshold = 5;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        INotificationService notificationService,
        ILedgerRepository ledgerRepository,
        IStringLocalizer<SharedResource> localizer,
        IMapper mapper,
        IValidator<SaleAddRequest> addValidator,
        IValidator<SaleUpdateRequest> updateValidator,
        IPlanLimitService planLimitService
    )
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _notificationService = notificationService;
        _ledgerRepository = ledgerRepository;
        _localizer = localizer;
        _planLimitService = planLimitService;
        _mapper = mapper;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    public async Task<SaleResponse> AddSaleAsync(SaleAddRequest saleAddRequest, Guid businessId)
    {
        ArgumentNullException.ThrowIfNull(saleAddRequest);

        var validation = await _addValidator.ValidateAsync(saleAddRequest);

        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
        await _planLimitService.EnsureCanCreateSaleAsync(businessId);

        Sale sale = _mapper.Map<Sale>(saleAddRequest);
        sale.Date = DateTime.UtcNow;

        sale.Items = await BuildSaleItemsAsync(saleAddRequest.Items, businessId);

        Sale added = await _saleRepository.AddAsync(sale, businessId);

        if (added.CustomerId.HasValue)
            await RecordLedgerEntriesForSaleAsync(added, businessId);

        return _mapper.Map<SaleResponse>(added);
    }

    public async Task<List<SaleResponse>> AddSalesAsync(
        IEnumerable<SaleAddRequest> saleAddRequests,
        Guid businessId
    )
    {
        ArgumentNullException.ThrowIfNull(saleAddRequests);

        var requestList = saleAddRequests.ToList();
        var sales = new List<Sale>();

        foreach (var request in requestList)
        {
            var validation = await _addValidator.ValidateAsync(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            Sale sale = _mapper.Map<Sale>(request);
            sale.Date = DateTime.UtcNow;
            sale.Items = await BuildSaleItemsAsync(request.Items, businessId);

            sales.Add(sale);
        }

        List<Sale> addedSales = await _saleRepository.AddRangeAsync(sales, businessId);

        foreach (var sale in addedSales.Where(s => s.CustomerId.HasValue))
            await RecordLedgerEntriesForSaleAsync(sale, businessId);

        return _mapper.Map<List<SaleResponse>>(addedSales);
    }

    private async Task RecordLedgerEntriesForSaleAsync(Sale sale, Guid businessId)
    {
        var now = DateTime.UtcNow;

        await _ledgerRepository.AddAsync(
            new LedgerEntry
            {
                Id = Guid.NewGuid(),
                CustomerId = sale.CustomerId!.Value,
                BusinessId = businessId,
                EntryType = LedgerEntryType.Udhar,
                Amount = sale.TotalAmount,
                Notes = $"Sale {sale.InvoiceNumber}",
                CreatedAt = now,
                SaleId = sale.Id,
            }
        );

        if (sale.PaymentStatus == PaymentStatus.Paid)
        {
            var settlementType =
                sale.PaymentMethod == PaymentMethod.Card
                    ? LedgerEntryType.Card
                    : LedgerEntryType.Cash;

            await _ledgerRepository.AddAsync(
                new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    CustomerId = sale.CustomerId!.Value,
                    BusinessId = businessId,
                    EntryType = settlementType,
                    Amount = sale.TotalAmount,
                    Notes = $"Payment for {sale.InvoiceNumber}",
                    CreatedAt = now,
                    SaleId = sale.Id,
                }
            );
        }
    }

    private async Task<List<SaleItem>> BuildSaleItemsAsync(
        List<SaleItemRequest> items,
        Guid businessId
    )
    {
        if (items is null || items.Count == 0)
            throw new ValidationException(_localizer["Sale_MustHaveAtLeastOneItem"]);

        var saleItems = new List<SaleItem>();

        foreach (var itemRequest in items)
        {
            var product =
                await _productRepository.GetByIdAsync(itemRequest.ProductId)
                ?? throw new KeyNotFoundException(_localizer["Sale_ProductNotFound", itemRequest.ProductId]);

            if (product.BusinessId != businessId)
                throw new KeyNotFoundException(_localizer["Sale_ProductNotFound", itemRequest.ProductId]);

            if (product.Stock < itemRequest.Quantity)
                throw new ValidationException(_localizer["Sale_InsufficientStock", product.ProductName]);

            saleItems.Add(
                new SaleItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = product.Price,
                }
            );

            var previousStock = product.Stock;
            product.Stock -= itemRequest.Quantity;
            await _productRepository.UpdateAsync(product);

            await NotifyIfStockThresholdCrossedAsync(product, previousStock, businessId);
        }

        return saleItems;
    }

    private async Task NotifyIfStockThresholdCrossedAsync(
        Product product,
        int previousStock,
        Guid businessId
    )
    {
        if (product.Stock <= 0 && previousStock > 0)
        {
            await TryNotifyAsync(
                new CreateNotificationRequest(
                    Target: NotificationTarget.Business,
                    Title: _localizer["Sale_Notification_OutOfStock_Title"],
                    Message: _localizer["Sale_Notification_OutOfStock_Message", product.ProductName],
                    Type: NotificationType.OutOfStock,
                    BusinessId: businessId,
                    ReferenceId: product.Id
                )
            );
        }
        else if (product.Stock <= LowStockThreshold && previousStock > LowStockThreshold)
        {
            await TryNotifyAsync(
                new CreateNotificationRequest(
                    Target: NotificationTarget.Business,
                    Title: _localizer["Sale_Notification_LowStock_Title"],
                    Message: _localizer["Sale_Notification_LowStock_Message", product.ProductName, product.Stock],
                    Type: NotificationType.LowStock,
                    BusinessId: businessId,
                    ReferenceId: product.Id
                )
            );
        }
    }

    private async Task TryNotifyAsync(CreateNotificationRequest request)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(request);
        }
        catch { }
    }

    public async Task<bool> DeleteSaleAsync(Guid businessId, Guid saleId)
    {
        Sale? existing = await _saleRepository.GetByIdAsync(businessId, saleId);

        if (existing is null)
            throw new KeyNotFoundException(_localizer["Sale_NotFound", saleId]);

        return await _saleRepository.DeleteAsync(saleId);
    }

    public async Task<List<SaleResponse>> GetAllSalesAsync(Guid businessId)
    {
        List<Sale> sales = await _saleRepository.GetByBusinessIdAsync(businessId);

        if (sales == null || !sales.Any())
            return new List<SaleResponse>();

        return _mapper.Map<List<SaleResponse>>(sales);
    }

    public async Task<decimal> GetMonthlyRevenueAsync(Guid businessId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = new DateOnly(today.Year, today.Month, 1);

        return await _saleRepository.GetTotalRevenueAsync(businessId, from, today);
    }

    public async Task<List<MonthlyRevenueResponse>> GetMonthlyRevenueAsync(
        Guid businessId,
        int year
    )
    {
        return await _saleRepository.GetMonthlyRevenueAsync(businessId, year);
    }

    public async Task<SaleResponse?> GetSaleByIdAsync(Guid businessId, Guid saleId)
    {
        var sale = await _saleRepository.GetByIdAsync(businessId, saleId);
        return sale == null ? null : _mapper.Map<SaleResponse>(sale);
    }

    public async Task<SaleResponse> GetSaleByProductNameAsync(string productName, Guid businessId)
    {
        var sale = await _saleRepository.GetByProductNameAsync(productName, businessId);

        if (sale == null)
            return null;

        return _mapper.Map<SaleResponse>(sale);
    }

    public async Task<List<SaleResponse>> GetTodaySalesAsync(Guid businessId)
    {
        List<Sale> sales = await _saleRepository.GetTodaySalesAsync(businessId);

        return _mapper.Map<List<SaleResponse>>(sales);
    }

    public async Task<int> GetTotalOrdersAsync(Guid businessId)
    {
        return await _saleRepository.GetSaleCountAsync(businessId);
    }

    public async Task<List<WeeklySalesResponse>> GetWeeklySalesAsync(Guid businessId)
    {
        return await _saleRepository.GetWeeklySalesAsync(businessId);
    }

    public async Task<PaginatedResponse<SaleResponse>> GetSalesPagedAsync(
        Guid businessId,
        int pageNumber,
        int pageSize
    )
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                _localizer["General.PageNumber.Invalid"]
            );

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                _localizer["General.PageSize.Invalid"]
            );

        var (items, totalCount) = await _saleRepository.GetPagedAsync(
            businessId,
            pageNumber,
            pageSize
        );
        var mapped = _mapper.Map<List<SaleResponse>>(items);

        return new PaginatedResponse<SaleResponse>(mapped, pageNumber, pageSize, totalCount);
    }

    public async Task<SaleResponse> UpdateSaleAsync(
        Guid businessId,
        Guid saleId,
        SaleUpdateRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existing =
            await _saleRepository.GetByIdAsync(businessId, saleId)
            ?? throw new KeyNotFoundException(_localizer["Sale_NotFound", saleId]);

        var newItems = await BuildUpdatedSaleItemsAsync(existing.Items, request.Items, businessId);

        var updatedSale = new Sale
        {
            Id = existing.Id,
            BusinessId = businessId,
            InvoiceNumber = existing.InvoiceNumber,
            Date = existing.Date,
            Note = request.Note,
            NoteUr = request.NoteUr,
            CustomerId = request.CustomerId,
            PaymentStatus = request.PaymentStatus,
            PaymentMethod = request.PaymentMethod,
            DiscountAmount = request.DiscountAmount,
            Items = newItems,
        };

        var saved = await _saleRepository.UpdateAsync(updatedSale);
        return _mapper.Map<SaleResponse>(saved);
    }

    private async Task<List<SaleItem>> BuildUpdatedSaleItemsAsync(
        ICollection<SaleItem> oldItems,
        List<SaleItemRequest> newItemRequests,
        Guid businessId
    )
    {
        if (newItemRequests is null || newItemRequests.Count == 0)
            throw new ValidationException(_localizer["Sale_MustHaveAtLeastOneItem"]);

        var oldByProduct = oldItems.ToDictionary(i => i.ProductId, i => i.Quantity);
        var newProductIds = newItemRequests.Select(i => i.ProductId).ToHashSet();
        var resultItems = new List<SaleItem>();

        foreach (
            var (productId, oldQty) in oldByProduct.Where(kv => !newProductIds.Contains(kv.Key))
        )
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product is null || product.BusinessId != businessId)
                continue;

            var previousStock = product.Stock;
            product.Stock += oldQty;
            await _productRepository.UpdateAsync(product);
            await NotifyIfStockThresholdCrossedAsync(product, previousStock, businessId);
        }

        foreach (var itemRequest in newItemRequests)
        {
            var product =
                await _productRepository.GetByIdAsync(itemRequest.ProductId)
                ?? throw new KeyNotFoundException(_localizer["Sale_ProductNotFound", itemRequest.ProductId]);

            if (product.BusinessId != businessId)
                throw new KeyNotFoundException(_localizer["Sale_ProductNotFound", itemRequest.ProductId]);

            oldByProduct.TryGetValue(itemRequest.ProductId, out var oldQty);
            var delta = itemRequest.Quantity - oldQty;

            var available = product.Stock + oldQty;
            if (itemRequest.Quantity > available)
                throw new ValidationException(_localizer["Sale_InsufficientStock", product.ProductName]);

            var previousStock = product.Stock;
            product.Stock -= delta;
            await _productRepository.UpdateAsync(product);
            await NotifyIfStockThresholdCrossedAsync(product, previousStock, businessId);

            resultItems.Add(
                new SaleItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = product.Price,
                }
            );
        }

        return resultItems;
    }
}