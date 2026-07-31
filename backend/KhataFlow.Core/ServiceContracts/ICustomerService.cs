using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ICustomerService
{
    Task<CustomerListResponse> GetAllCustomersAsync(Guid BusinessId);

    Task<CustomerResponse?> GetCustomerByIdAsync(Guid id);       
    Task<CustomerListResponse?> GetCustomerByNameAsync(string name, Guid businessId);
    Task<PaginatedCustomerResponse> GetCustomersPagedAsync(Guid businessId, int pageNumber, int pageSize);
    Task<CustomerResponse> AddCustomerAsync(CustomerAddRequest customerAddRequest);

    Task<CustomerResponse> UpdateCustomerAsync(CustomerUpdateRequest customerUpdateRequest);

    Task<bool> DeleteCustomerAsync(Guid id);
}