using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public class SaleUpdateRequest
{
    public Guid? CustomerId { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Note { get; set; }
    public string? NoteUr { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public List<SaleItemRequest> Items { get; set; } = new();
}