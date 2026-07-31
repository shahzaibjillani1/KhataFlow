using Microsoft.EntityFrameworkCore;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Infrastructure.Data;
using KhataFlow.Core.DTO;

namespace KhataFlow.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product> AddAsync(Product product)
    {
        if (product.Category != null)
        {
            _context.Entry(product.Category).State = EntityState.Unchanged;
        }

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            return false;

        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid businessId, string name)
    {
        return await _context.Products
            .AnyAsync(p =>
                p.BusinessId == businessId &&
                EF.Functions.Like(p.ProductName, name));
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByIdWithCategoryAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllWithCategoryAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetByBusinessIdAsync(Guid businessId)
    {
        return await _context.Products
            .Where(p => p.BusinessId == businessId)
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetByIdForBusinessAsync(Guid id, Guid businessId)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId && !p.IsDeleted);
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        Guid businessId, int pageNumber, int pageSize)
    {
        var query = _context.Products
            .Where(p => p.BusinessId == businessId)
            .Include(p => p.Category)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.ProductName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Product>> GetByNameAsync(Guid businessId, string name)
    {
        return await _context.Products
            .Where(p =>
                p.BusinessId == businessId &&
                EF.Functions.Like(p.ProductName, $"%{name}%"))
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetLowStockAsync(Guid businessId, int threshold)
    {
        return await _context.Products
            .Where(p => p.BusinessId == businessId && p.Stock > 0 && p.Stock <= threshold)
            .OrderBy(p => p.Stock)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetTopProductsBySalesAsync(Guid businessId, int topN)
    {
        return await _context.Products
            .Where(p => p.BusinessId == businessId)
            .Include(p => p.Category)
            .Include(p => p.SaleItems)
            .AsNoTracking()
            .OrderByDescending(p => p.SaleItems.Sum(si => si.Quantity))
            .Take(topN)
            .ToListAsync();
    }

    public async Task<int> GetProductCountAsync(Guid businessId)
    {
        return await _context.Products
            .CountAsync(p => p.BusinessId == businessId);
    }

    public async Task<int> GetLowStockCountAsync(Guid businessId, int threshold)
    {
        return await _context.Products
            .CountAsync(p =>
                p.BusinessId == businessId &&
                p.Stock > 0 &&
                p.Stock <= threshold);
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(Guid categoryId, Guid businessId)
    {
return await _context.Products
            .Where(p => p.CategoryId == categoryId && p.BusinessId == businessId)
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync();
    }
    public async Task<Product> UpdateAsync(Product product)
    {
        var existing = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id)
            ?? throw new KeyNotFoundException($"Product '{product.Id}' not found.");

        existing.ProductName = product.ProductName;
        existing.ProductNameUr = product.ProductNameUr;
        existing.CategoryId = product.CategoryId;
        existing.Price = product.Price;
        existing.Stock = product.Stock;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<List<Product>> GetLowStockProductsAsync(Guid businessId)
    {
        return await _context.Products
            .Where(p => p.BusinessId == businessId && p.Stock > 0 && p.Stock <= p.LowStockThreshold)
            .OrderBy(p => p.Stock)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetInStockProductsAsync(Guid businessId)
    {
        return await _context.Products
            .Where(p => p.BusinessId == businessId && p.Stock > 0)
            .OrderBy(p => p.Stock)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetOutOfStockProductsAsync(Guid businessId)
    {
        return await _context.Products
            .Where(p => p.BusinessId == businessId && p.Stock == 0)
            .OrderBy(p => p.Stock)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetLowStockCountAsync(Guid businessId)
    {
        return await _context.Products
            .CountAsync(p => p.BusinessId == businessId && p.Stock > 0 && p.Stock <= p.LowStockThreshold);
    }

    public async Task<int> CountByBusinessAsync(Guid businessId)
    => await _context.Products
        .Where(p => p.BusinessId == businessId)
        .CountAsync();
}