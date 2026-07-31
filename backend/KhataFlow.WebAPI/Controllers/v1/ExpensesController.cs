using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class ExpensesController : CustomControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public ExpensesController(
        IExpenseService expenseService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _expenseService = expenseService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllExpenses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var businessId = GetBusinessId();
        var result = await _expenseService.GetExpensesPagedAsync(businessId, pageNumber, pageSize);
        return Success(result, _localizer["Expense.GetAll.Success"]);
    }

    [HttpPost]
    public async Task<IActionResult> AddExpense([FromBody] ExpenseAddRequest request)
    {
        var businessId = GetBusinessId();

        try
        {
            var expense = await _expenseService.AddExpenseAsync(businessId, request);
            return Created(expense, _localizer["Expense.Create.Success"]);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpGet("total")]
    public async Task<IActionResult> GetTotal([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var businessId = GetBusinessId();
        var total = await _expenseService.GetTotalExpensesAsync(businessId, from, to);
        return Success(total, _localizer["Expense.Total.Success"]);
    }

    [HttpGet("by-category")]
    public async Task<IActionResult> GetByCategory([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var businessId = GetBusinessId();
        var breakdown = await _expenseService.GetCategoryBreakdownAsync(businessId, from, to);
        return Success(breakdown, _localizer["Expense.ByCategory.Success"]);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        var businessId = GetBusinessId();
        var deleted = await _expenseService.DeleteExpenseAsync(businessId, id);
        return Success(deleted, _localizer["Expense.Delete.Success"]);
    }
}