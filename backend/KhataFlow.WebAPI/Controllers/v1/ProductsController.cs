using Asp.Versioning;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KhataFlow.WebAPI.Controllers.v1;

public class ProductsController : CustomControllerBase
{
    private readonly IProductService _productService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IAIClientService _aiClient;

    public ProductsController(
        IProductService productService,
        IStringLocalizer<SharedResource> localizer,
        IAIClientService aiClient)
        : base(localizer)
    {
        _productService = productService;
        _localizer = localizer;
        _aiClient = aiClient;
    }

    private Task<string> TranslateDynamicAsync(string englishMessage)
    {
        var targetLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return _aiClient.TranslateAsync(englishMessage, targetLanguage, HttpContext.RequestAborted);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var businessId = GetBusinessId();
        var products = await _productService.GetProductsPagedAsync(businessId, pageNumber, pageSize);

        return Success(products, _localizer["Product.GetAll.Success"]);
    }

    [HttpGet("low-stock/count")]
    public async Task<IActionResult> GetLowStockCount()
    {
        var businessId = GetBusinessId();
        var count = await _productService.GetLowStockProductsCountAsync(businessId);

        return Success(count, _localizer["Product.LowStockCount.Success"]);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        var businessId = GetBusinessId();
        var products = await _productService.GetLowStockProductsAsync(businessId);

        return Success(products, _localizer["Product.LowStock.Success"]);
    }

    [HttpGet("in-stock")]
    public async Task<IActionResult> GetInStockProducts()
    {
        var businessId = GetBusinessId();
        var products = await _productService.GetInStockProductsAsync(businessId);

        return Success(products, _localizer["Product.InStock.Success"]);
    }

    [HttpGet("out-of-stock")]
    public async Task<IActionResult> GetOutOfStockProducts()
    {
        var businessId = GetBusinessId();
        var products = await _productService.GetOutOfStockProductsAsync(businessId);

        return Success(products, _localizer["Product.OutOfStock.Success"]);
    }

    [HttpGet("top-sales")]
    public async Task<IActionResult> GetTopSalesProducts()
    {
        var businessId = GetBusinessId();
        var products = await _productService.GetTopProductsBySalesAsync(businessId);

        return Success(products, _localizer["Product.TopSales.Success"]);
    }

    [HttpGet("category/{categoryId:guid}")]
    public async Task<IActionResult> GetProductsByCategory(Guid categoryId)
    {
        var businessId = GetBusinessId();
        var products = await _productService.GetProductsByCategoryAsync(businessId, categoryId);

        return Success(products, _localizer["Product.ByCategory.Success"]);
    }

    [HttpGet("{productName}")]
    public async Task<IActionResult> GetProductByName(string productName)
    {
        var businessId = GetBusinessId();
        var products = await _productService.GetProductByNameAsync(productName, businessId);

        return Success(products, _localizer["Product.GetByName.Success"]);
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] ProductAddRequest request)
    {
        var businessId = GetBusinessId();

        try
        {
            var product = await _productService.AddProductAsync(request, businessId);
            return Created(product, _localizer["Product.Create.Success"]);
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
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductUpdateRequest request)
    {
        var businessId = GetBusinessId();
        var requestWithId = request with { id = id };

        try
        {
            var product = await _productService.UpdateProductAsync(requestWithId, businessId);
            return Success(product, _localizer["Product.Update.Success"]);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse(_localizer["Product.NotFound", id]);
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
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var businessId = GetBusinessId();
        var deleted = await _productService.DeleteProductAsync(id, businessId);
        return Success(deleted, _localizer["Product.Delete.Success"]);
    }
}