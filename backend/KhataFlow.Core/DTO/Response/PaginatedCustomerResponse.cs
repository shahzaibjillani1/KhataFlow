namespace KhataFlow.Core.DTO.Response;

public record PaginatedCustomerResponse(
    List<CustomerResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    decimal TotalOutstanding
);