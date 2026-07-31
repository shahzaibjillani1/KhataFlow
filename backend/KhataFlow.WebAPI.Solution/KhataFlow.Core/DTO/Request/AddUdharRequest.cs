namespace KhataFlow.Core.DTO;

public record AddUdharRequest
{
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
}