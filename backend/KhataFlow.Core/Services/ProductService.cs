using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPlanLimitService _planLimitService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly IValidator<ProductAddRequest> _addValidator;
    private readonly IValidator<ProductUpdateRequest> _updateValidator;
    private readonly IBilingualTextService _bilingual;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private const int LowStockThreshold = 5;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IPlanLimitService planLimitService,
        INotificationService notificationService,
        IMapper mapper,
        IValidator<ProductAddRequest> addValidator,
        IValidator<ProductUpdateRequest> updateValidator,
        IBilingualTextService bilingual,
        IStringLocalizer<SharedResource> localizer)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _notificationService = notificationService;
        _planLimitService = planLimitService;
        _mapper = mapper;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
        _bilingual = bilingual;
        _localizer = localizer;
    }

    public async Task<ProductResponse> AddProductAsync(ProductAddRequest request, Guid businessId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await _addValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
        await _planLimitService.EnsureCanAddProductAsync(businessId);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, businessId)
            ?? throw new KeyNotFoundException(_localizer["Category.NotFound", request.CategoryId]);

        var product = _mapper.Map<Product>(request);
        product.BusinessId = businessId;
        product.Category = category;

        (product.ProductName, product.ProductNameUr) = await _bilingual.ResolveAsync(request.ProductName);

        var created = await _productRepository.AddAsync(product);

        if (created.Stock <= LowStockThreshold)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Business,
                Title: _localizer["Product.Notification.NewLowStock.Title"],
                Message: string.Format(_localizer["Product.Notification.NewLowStock.Message"], created.ProductName, created.Stock),
                Type: NotificationType.LowStock,
                BusinessId: businessId,
                ReferenceId: created.Id));
        }

        var withCategory = await _productRepository.GetByIdWithCategoryAsync(created.Id)
            ?? throw new Exception(_localizer["Product.ReloadFailed.Add"]);

        return _mapper.Map<ProductResponse>(withCategory);
    }

    public async Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest request, Guid businessId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existing = await _productRepository.GetByIdForBusinessAsync(request.id, businessId)
            ?? throw new KeyNotFoundException(_localizer["Product.NotFound", request.id]);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, businessId)
            ?? throw new KeyNotFoundException(_localizer["Category.NotFound", request.CategoryId]);

        var previousStock = existing.Stock;

        bool nameChanged = _bilingual.ContainsUrduScript(request.ProductName)
            ? !string.Equals(existing.ProductNameUr, request.ProductName, StringComparison.Ordinal)
            : !string.Equals(existing.ProductName, request.ProductName, StringComparison.Ordinal);

        bool nameUrStale = _bilingual.IsTranslationStale(existing.ProductName, existing.ProductNameUr);

        _mapper.Map(request, existing);

        if (nameChanged || nameUrStale)
            (existing.ProductName, existing.ProductNameUr) = await _bilingual.ResolveAsync(request.ProductName);

        var updated = await _productRepository.UpdateAsync(existing);

        await NotifyStockChangeAsync(updated, previousStock, businessId);

        var withCategory = await _productRepository.GetByIdWithCategoryAsync(updated.Id)
            ?? throw new Exception(_localizer["Product.ReloadFailed.Update"]);
        return _mapper.Map<ProductResponse>(withCategory!);
    }

    public async Task<List<ProductResponse>?> GetProductByNameAsync(string productName, Guid businessId)
    {
        var products = await _productRepository.GetByNameAsync(businessId, productName);
        return products == null || !products.Any()
            ? null
            : _mapper.Map<List<ProductResponse>>(products);
    }

    public async Task<PaginatedResponse<ProductResponse>> GetProductsPagedAsync(
        Guid businessId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), _localizer["Product.PageNumber.Invalid"]);

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), _localizer["Product.PageSize.Invalid"]);

        var (items, totalCount) = await _productRepository.GetPagedAsync(businessId, pageNumber, pageSize);
        var mapped = _mapper.Map<List<ProductResponse>>(items);

        return new PaginatedResponse<ProductResponse>(mapped, pageNumber, pageSize, totalCount);
    }

    public async Task<List<ProductResponse>> GetTopProductsBySalesAsync(Guid businessId, int topN = 5)
    {
        if (topN <= 0)
            throw new ArgumentOutOfRangeException(nameof(topN), _localizer["Product.TopN.Invalid"]);

        var products = await _productRepository.GetTopProductsBySalesAsync(businessId, topN);
        return _mapper.Map<List<ProductResponse>>(products);
    }

    public async Task<bool> DeleteProductAsync(Guid productId, Guid businessId)
    {
        var existing = await _productRepository.GetByIdForBusinessAsync(productId, businessId)
            ?? throw new KeyNotFoundException(_localizer["Product.NotFound", productId]);

        return await _productRepository.DeleteAsync(productId);
    }

    public async Task<int> GetProductCountAsync(Guid businessId)
        => await _productRepository.GetProductCountAsync(businessId);

    public async Task<int> GetLowStockProductsCountAsync(Guid businessId, int threshold = 5)
        => await _productRepository.GetLowStockCountAsync(businessId, threshold);

    public async Task<List<ProductResponse>> GetProductsByCategoryAsync(Guid businessId, Guid categoryId)
    {
        var products = await _productRepository.GetProductsByCategoryAsync(businessId, categoryId);
        return _mapper.Map<List<ProductResponse>>(products);
    }

    public async Task<List<ProductResponse>> GetLowStockProductsAsync(Guid businessId)
    {
        var products = await _productRepository.GetLowStockProductsAsync(businessId);
        return _mapper.Map<List<ProductResponse>>(products);
    }

    public async Task<List<ProductResponse>> GetInStockProductsAsync(Guid businessId)
    {
        var products = await _productRepository.GetInStockProductsAsync(businessId);
        return _mapper.Map<List<ProductResponse>>(products);
    }

    public async Task<List<ProductResponse>> GetOutOfStockProductsAsync(Guid businessId)
    {
        var products = await _productRepository.GetOutOfStockProductsAsync(businessId);
        return _mapper.Map<List<ProductResponse>>(products);
    }

    private async Task NotifyStockChangeAsync(Product product, int previousStock, Guid businessId)
    {
        if (product.Stock == previousStock)
            return;

        if (product.Stock <= 0 && previousStock > 0)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Business,
                Title: _localizer["Product.Notification.OutOfStock.Title"],
                Message: string.Format(_localizer["Product.Notification.OutOfStock.Message"], product.ProductName),
                Type: NotificationType.OutOfStock,
                BusinessId: businessId,
                ReferenceId: product.Id));
        }
        else if (product.Stock <= LowStockThreshold && previousStock > LowStockThreshold)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Business,
                Title: _localizer["Product.Notification.LowStock.Title"],
                Message: string.Format(_localizer["Product.Notification.LowStock.Message"], product.ProductName, product.Stock),
                Type: NotificationType.LowStock,
                BusinessId: businessId,
                ReferenceId: product.Id));
        }
        else if (product.Stock > LowStockThreshold && previousStock <= LowStockThreshold)
        {
            await TryNotifyAsync(new CreateNotificationRequest(
                Target: NotificationTarget.Business,
                Title: _localizer["Product.Notification.Restocked.Title"],
                Message: string.Format(_localizer["Product.Notification.Restocked.Message"], product.ProductName, product.Stock),
                Type: NotificationType.ProductRestocked,
                BusinessId: businessId,
                ReferenceId: product.Id));
        }
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
}