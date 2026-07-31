using Microsoft.EntityFrameworkCore;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Infrastructure.Data;

namespace KhataFlow.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer> AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<int> CountByBusinessAsync(Guid businessId)
    => await _context.Customers
        .Where(c => c.BusinessId == businessId)
        .CountAsync();

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null || customer.IsDeleted)
            return false;

        customer.IsDeleted = true;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid businessId, string phone)
    {
        return await _context.Customers
            .AnyAsync(c =>
                c.BusinessId == businessId &&
                c.PhoneNumber == phone &&
                !c.IsDeleted);
    }

    public async Task<List<Customer>> GetByBusinessIdAsync(Guid businessId)
    {
        return await _context.Customers
            .Where(c => c.BusinessId == businessId && !c.IsDeleted)
            .Include(c => c.LedgerEntries)
            .Include(c => c.Sales)
                .ThenInclude(s => s.Items)   
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _context.Customers
            .Include(c => c.LedgerEntries)
            .Include(c => c.Sales)
                .ThenInclude(s => s.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<Customer?> GetByPhoneAsync(Guid businessId, string phone)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.BusinessId == businessId &&
                c.PhoneNumber == phone &&
                !c.IsDeleted);
    }

    public async Task<Customer?> GetByPublicTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .Include(c => c.Business)
            .Include(c => c.LedgerEntries)
            .FirstOrDefaultAsync(c => c.PublicToken == token, ct);
    }

    public async Task<int> GetCustomerCountAsync(Guid businessId)
    {
        return await _context.Customers
            .CountAsync(c => c.BusinessId == businessId && !c.IsDeleted);
    }

    public async Task<List<Customer>> GetCustomersWithOutstandingAsync(Guid businessId)
    {
        
        var customers = await _context.Customers
            .Where(c => c.BusinessId == businessId && !c.IsDeleted)
            .Include(c => c.LedgerEntries)
            .AsNoTracking()
            .ToListAsync();

        return customers
            .Where(c => c.OutstandingBalance > 0)
            .ToList();
    }

    public async Task<(List<Customer>, int)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize)
    {
        var query = _context.Customers
            .Where(c => c.BusinessId == businessId && !c.IsDeleted);

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(c => c.LedgerEntries)
            .Include(c => c.Sales)
                .ThenInclude(s => s.Items)   
            .OrderBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        var existing = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customer.Id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"Customer '{customer.Id}' not found.");

        existing.Name = customer.Name;
        existing.NameUr = customer.NameUr;
        existing.Address = customer.Address;
        existing.AddressUr = customer.AddressUr;
        existing.PhoneNumber = customer.PhoneNumber;
        existing.LastVisit = customer.LastVisit;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }
}