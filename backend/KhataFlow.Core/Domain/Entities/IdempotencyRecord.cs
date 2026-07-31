using KhataFlow.Core.Domain.Common;

namespace KhataFlow.Core.Domain.Entities;

public class IdempotencyRecord : BaseEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}