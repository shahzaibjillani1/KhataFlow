using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface IIdempotencyRecordRepository
{
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken ct = default);
    Task AddAsync(IdempotencyRecord record, CancellationToken ct = default);
}