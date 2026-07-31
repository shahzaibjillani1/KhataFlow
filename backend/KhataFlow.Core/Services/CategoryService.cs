using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CategoryAddRequest> _addValidator;
    private readonly IValidator<CategoryUpdateRequest> _updateValidator;
    private readonly IBilingualTextService _bilingual;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IMapper mapper,
        IValidator<CategoryAddRequest> addValidator,
        IValidator<CategoryUpdateRequest> updateValidator,
        IBilingualTextService bilingual,
        IStringLocalizer<SharedResource> localizer
    )
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
        _bilingual = bilingual;
        _localizer = localizer;
    }

    public async Task<CategoryResponse> AddCategoryAsync(
        Guid businessId,
        CategoryAddRequest request
    )
    {
        var validation = await _addValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var duplicate = await _categoryRepository.ExistsAsync(businessId, request.CategoryName);
        if (duplicate)
            throw new DomainException(_localizer["Category.NotFound", request.CategoryName]);

        var category = _mapper.Map<Category>(request);

        category.Id = Guid.NewGuid();
        category.BusinessId = businessId;
        category.CreatedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;

        (category.CategoryName, category.CategoryNameUr) = await _bilingual.ResolveAsync(
            request.CategoryName
        );

        var created = await _categoryRepository.AddAsync(category, businessId);
        return _mapper.Map<CategoryResponse>(created);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(
        Guid businessId,
        CategoryUpdateRequest request
    )
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existing =
            await _categoryRepository.GetByIdAsync(request.Id, businessId)
            ?? throw new NotFoundException(_localizer["Category.NotFound", request.Id]);

        var duplicate = await _categoryRepository.ExistsAsync(businessId, request.CategoryName);
        if (
            duplicate
            && !existing.CategoryName.Equals(
                request.CategoryName,
                StringComparison.OrdinalIgnoreCase
            )
        )
            throw new DomainException(_localizer["Category.NotFound", request.CategoryName]);

        bool nameChanged = _bilingual.ContainsUrduScript(request.CategoryName)
            ? !string.Equals(
                existing.CategoryNameUr,
                request.CategoryName,
                StringComparison.Ordinal
            )
            : !string.Equals(existing.CategoryName, request.CategoryName, StringComparison.Ordinal);

        bool nameUrStale = _bilingual.IsTranslationStale(
            existing.CategoryName,
            existing.CategoryNameUr
        );

        _mapper.Map(request, existing);
        existing.UpdatedAt = DateTime.UtcNow;

        if (nameChanged || nameUrStale)
            (existing.CategoryName, existing.CategoryNameUr) = await _bilingual.ResolveAsync(
                request.CategoryName
            );

        var updated = await _categoryRepository.UpdateAsync(existing, businessId);
        return _mapper.Map<CategoryResponse>(updated);
    }

    public async Task<bool> DeleteCategoryAsync(Guid businessId, Guid id)
    {
        var existing =
            await _categoryRepository.GetByIdAsync(id, businessId)
            ?? throw new NotFoundException(_localizer["Category.NotFound", id]);

        return await _categoryRepository.DeleteAsync(id, businessId);
    }

    public async Task<List<CategoryResponse>> GetAllCategoriesAsync(Guid businessId)
    {
        var categories = await _categoryRepository.GetByBusinessIdAsync(businessId);
        return _mapper.Map<List<CategoryResponse>>(categories);
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(Guid businessId, Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id, businessId);
        return category is null ? null : _mapper.Map<CategoryResponse>(category);
    }

    public async Task<PaginatedResponse<CategoryResponse>> GetCategoriesPagedAsync(
    Guid businessId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), _localizer["Category.PageNumber.Invalid"]);

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), _localizer["Category.PageSize.Invalid"]);

        var (items, totalCount) = await _categoryRepository.GetPagedAsync(businessId, pageNumber, pageSize);
        var mapped = _mapper.Map<List<CategoryResponse>>(items);

        return new PaginatedResponse<CategoryResponse>(mapped, pageNumber, pageSize, totalCount);
    }
}
