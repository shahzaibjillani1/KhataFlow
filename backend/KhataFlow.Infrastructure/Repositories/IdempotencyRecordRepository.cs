using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Repositories;

public class IdempotencyRecordRepository : IIdempotencyRecordRepository
{
    private readonly AppDbContext _context;

    public IdempotencyRecordRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string idempotencyKey, CancellationToken ct = default)
    {
        return await _context.IdempotencyRecords
            .AnyAsync(r => r.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task AddAsync(IdempotencyRecord record, CancellationToken ct = default)
    {
        await _context.IdempotencyRecords.AddAsync(record, ct);
        await _context.SaveChangesAsync(ct);
    }
}