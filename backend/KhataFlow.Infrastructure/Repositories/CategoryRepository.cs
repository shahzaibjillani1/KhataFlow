using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Exceptions;
using KhataFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Category> AddAsync(Category category, Guid businessId)
    {
        var businessExists = await _context.Businesses.AnyAsync(b => b.Id == businessId);

        if (!businessExists)
            throw new NotFoundException(
                $"Business '{businessId}' not found. "
                    + $"Create a business first before adding categories."
            );

        var nameExists = await _context.Categories.AnyAsync(c =>
            c.BusinessId == businessId && c.CategoryName == category.CategoryName
        );

        if (nameExists)
            throw new ConflictException($"Category '{category.CategoryName}' already exists.");

        category.BusinessId = businessId;
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<Category?> GetByIdAsync(Guid id, Guid businessId)
    {
        return await _context
            .Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);
    }

    public async Task<List<Category>> GetByBusinessIdAsync(Guid businessId)
    {
        return await _context
            .Categories.Where(c => c.BusinessId == businessId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid businessId, string name)
    {
        return await _context.Categories.AnyAsync(c =>
            c.BusinessId == businessId && c.CategoryName == name
        );
    }

    public async Task<Category> UpdateAsync(Category category, Guid businessId)
    {
        var businessExists = await _context.Businesses.AnyAsync(b => b.Id == businessId);

        if (!businessExists)
            throw new NotFoundException($"Business '{businessId}' not found.");

        var existing =
            await _context.Categories.FirstOrDefaultAsync(c =>
                c.Id == category.Id && c.BusinessId == businessId
            ) ?? throw new NotFoundException($"Category '{category.Id}' not found.");

        if (
            !string.Equals(
                existing.CategoryName,
                category.CategoryName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            var nameExists = await _context.Categories.AnyAsync(c =>
                c.BusinessId == businessId
                && c.CategoryName == category.CategoryName
                && c.Id != category.Id
            );

            if (nameExists)
                throw new ConflictException($"Category '{category.CategoryName}' already exists.");
        }

        existing.CategoryName = category.CategoryName;
        existing.CategoryNameUr = category.CategoryNameUr;
        existing.Description = category.Description;
        existing.DescriptionUr = category.DescriptionUr;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid businessId)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c =>
            c.Id == id && c.BusinessId == businessId
        );

        if (category is null)
            return false;

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(List<Category>, int)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize)
    {
        var query = _context.Categories.Where(c => c.BusinessId == businessId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.CategoryName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
