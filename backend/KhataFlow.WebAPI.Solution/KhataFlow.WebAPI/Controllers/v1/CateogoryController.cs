using KhataFlow.Core.DTO;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class CategoryController : CustomControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public CategoryController(
        ICategoryService categoryService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _categoryService = categoryService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    /// <summary>
    /// Our own fixed copy goes through the resx localizer above. Error text
    /// thrown dynamically by the service layer isn't pre-populated in the
    /// resx, so it's translated on the fly instead. No-op unless the current
    /// UI culture is Urdu.
    /// </summary>
    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCategories([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var businessId = GetBusinessId();
        var result = await _categoryService.GetCategoriesPagedAsync(businessId, pageNumber, pageSize);
        return Success(result, _localizer["Category.GetAll.Success"]);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var businessId = GetBusinessId();
        var category = await _categoryService.GetCategoryByIdAsync(businessId, id);

        if (category is null)
            return NotFoundResponse(_localizer["Category.NotFound", id]);

        return Success(category, _localizer["Category.GetById.Success"]);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryAddRequest request)
    {
        var businessId = GetBusinessId();

        try
        {
            var category = await _categoryService.AddCategoryAsync(businessId, request);
            return Created(category, _localizer["Category.Create.Success"]);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid id, [FromBody] CategoryUpdateRequest request)
    {
        if (id != request.Id)
            return BadRequestResponse(_localizer["Category.Update.IdMismatch"]);

        var businessId = GetBusinessId();

        try
        {
            var category = await _categoryService.UpdateCategoryAsync(businessId, request);
            return Success(category, _localizer["Category.Update.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Category.NotFound", id]);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResponse(await TranslateDynamicAsync(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(await TranslateDynamicAsync(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var businessId = GetBusinessId();
        var deleted = await _categoryService.DeleteCategoryAsync(businessId, id);

        if (!deleted)
            return NotFoundResponse(_localizer["Category.NotFound", id]);

        return Success(deleted, _localizer["Category.Delete.Success"]);
    }
}