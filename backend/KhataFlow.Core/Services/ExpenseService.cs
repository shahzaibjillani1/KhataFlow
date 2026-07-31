using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<ExpenseAddRequest> _addValidator;
    private readonly IAIClientService _aiClient;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ExpenseService(
        IExpenseRepository expenseRepository,
        IMapper mapper,
        IValidator<ExpenseAddRequest> addValidator,
        IAIClientService aiClient,
        IStringLocalizer<SharedResource> localizer)
    {
        _expenseRepository = expenseRepository;
        _mapper = mapper;
        _addValidator = addValidator;
        _aiClient = aiClient;
        _localizer = localizer;
    }

    public async Task<ExpenseResponse> AddExpenseAsync(Guid businessId, ExpenseAddRequest request)
    {
        var validation = await _addValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Title = request.Title,
            Amount = request.Amount,
            Category = request.Category,
            Note = request.Note,
            Date = request.Date ?? DateTime.UtcNow
        };

        expense.TitleUr = await _aiClient.TranslateAsync(expense.Title, "ur");

        if (!string.IsNullOrWhiteSpace(expense.Note))
            expense.NoteUr = await _aiClient.TranslateAsync(expense.Note, "ur");

        var added = await _expenseRepository.AddAsync(expense);

        return _mapper.Map<ExpenseResponse>(added);
    }

    public async Task<List<ExpenseResponse>> GetAllExpensesAsync(Guid businessId)
    {
        var expenses = await _expenseRepository.GetByBusinessIdAsync(businessId);
        return _mapper.Map<List<ExpenseResponse>>(expenses);
    }

    public async Task<decimal> GetTotalExpensesAsync(Guid businessId, DateOnly from, DateOnly to)
    {
        return await _expenseRepository.GetTotalInRangeAsync(businessId, from, to);
    }

    public async Task<List<CategoryExpenseSummaryResponse>> GetCategoryBreakdownAsync(Guid businessId, DateOnly from, DateOnly to)
    {
        var totals = await _expenseRepository.GetTotalsByCategoryInRangeAsync(businessId, from, to);
        return totals
            .Select(kvp => new CategoryExpenseSummaryResponse(kvp.Key, kvp.Value))
            .OrderByDescending(x => x.Total)
            .ToList();
    }

    public async Task<bool> DeleteExpenseAsync(Guid businessId, Guid expenseId)
    {
        var existing = await _expenseRepository.GetByIdAsync(expenseId, businessId)
            ?? throw new NotFoundException(_localizer["Expense.NotFound", expenseId]);

        return await _expenseRepository.DeleteAsync(expenseId);
    }

    public async Task<PaginatedResponse<ExpenseResponse>> GetExpensesPagedAsync(
        Guid businessId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), _localizer["General.PageNumber.Invalid"]);

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), _localizer["General.PageSize.Invalid"]);

        var (items, totalCount) = await _expenseRepository.GetPagedAsync(businessId, pageNumber, pageSize);
        var mapped = _mapper.Map<List<ExpenseResponse>>(items);

        return new PaginatedResponse<ExpenseResponse>(mapped, pageNumber, pageSize, totalCount);
    }
}