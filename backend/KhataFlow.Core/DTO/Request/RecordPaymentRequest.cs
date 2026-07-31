namespace KhataFlow.Core.DTO.Request;

public record RecordPaymentRequest
{
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
}
