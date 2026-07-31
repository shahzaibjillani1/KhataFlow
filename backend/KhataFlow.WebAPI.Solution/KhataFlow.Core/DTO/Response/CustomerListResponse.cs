namespace KhataFlow.Core.DTO.Response;
public class CustomerListResponse
{
    public List<CustomerResponse> Customers { get; set; } = [];
    public int TotalCustomers { get; set; }
    public decimal TotalOutstanding { get; set; }
}
