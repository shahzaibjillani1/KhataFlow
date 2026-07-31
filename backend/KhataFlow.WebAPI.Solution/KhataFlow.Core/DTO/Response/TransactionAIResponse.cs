namespace KhataFlow.Core.DTO.Response;

public class TransactionAIResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public string? TransactionType { get; set; } 
    public decimal? Amount { get; set; }
    public string? Currency { get; set; } = "PKR";
    public string? Person { get; set; }
    public string? Category { get; set; }
    public DateTime? Date { get; set; }
    public string? Description { get; set; }

}
