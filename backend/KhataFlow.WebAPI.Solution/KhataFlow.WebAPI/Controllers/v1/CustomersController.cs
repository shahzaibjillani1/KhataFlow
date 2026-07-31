using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class CustomersController : CustomControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public CustomersController(
        ICustomerService customerService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _customerService = customerService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCustomers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var businessId = GetBusinessId();
        var result = await _customerService.GetCustomersPagedAsync(businessId, pageNumber, pageSize);
        return Success(result, _localizer["Customer.GetAll.Success"]);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomerById(Guid id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);

        if (customer is null)
            return NotFoundResponse(_localizer["Customer.NotFoundById", id]);

        return Success(customer, _localizer["Customer.GetById.Success"]);
    }

    [HttpGet("search")]
    public async Task<IActionResult> GetCustomerByName([FromQuery] string name)
    {
        var businessId = GetBusinessId();
        var customer = await _customerService.GetCustomerByNameAsync(name, businessId);

        if (customer is null)
            return NotFound(new ApiResponse<object>
            {
                Message = _localizer["Customer.NotFoundByName", name],
                Result = false,
                Data = null
            });

        return Success(customer, _localizer["Customer.GetById.Success"]);
    }

    [HttpPost]
    public async Task<IActionResult> AddCustomer([FromBody] CustomerAddRequest request)
    {
        var businessId = GetBusinessId();

        try
        {
            var newCustomer = await _customerService.AddCustomerAsync(request with { BusinessId = businessId });
            return Created(newCustomer, _localizer["Customer.Create.Success"]);
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
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] CustomerUpdateRequest request)
    {
        if (id != request.Id)
            return BadRequestResponse(_localizer["Customer.Update.IdMismatch"]);

        try
        {
            var updatedCustomer = await _customerService.UpdateCustomerAsync(request);
            return Success(updatedCustomer, _localizer["Customer.Update.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Customer.NotFoundById", id]);
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
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var deleted = await _customerService.DeleteCustomerAsync(id);

        if (!deleted)
            return StatusCode(500, new ApiResponse<object>
            {
                Message = _localizer["Customer.Delete.Error"],
                Result = false,
                Data = null
            });

        return Success(deleted, _localizer["Customer.Delete.Success"]);
    }
}