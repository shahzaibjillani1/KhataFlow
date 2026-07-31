using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPlanLimitService _planLimitService;
    private readonly IMapper _mapper;
    private readonly IValidator<CustomerAddRequest> _addValidator;
    private readonly IValidator<CustomerUpdateRequest> _updateValidator;
    private readonly IBilingualTextService _bilingual;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPlanLimitService planLimitService,
        IMapper mapper,
        IValidator<CustomerAddRequest> addValidator,
        IValidator<CustomerUpdateRequest> updateValidator,
        IBilingualTextService bilingual,
        IStringLocalizer<SharedResource> localizer)
    {
        _customerRepository = customerRepository;
        _planLimitService = planLimitService;
        _mapper = mapper;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
        _bilingual = bilingual;
        _localizer = localizer;
    }

    public async Task<CustomerResponse> AddCustomerAsync(CustomerAddRequest customerAddRequest)
    {
        ArgumentNullException.ThrowIfNull(customerAddRequest);

        var validation = await _addValidator.ValidateAsync(customerAddRequest);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
        await _planLimitService.EnsureCanAddCustomerAsync(customerAddRequest.BusinessId);

        bool exists = await _customerRepository.ExistsAsync(customerAddRequest.BusinessId, customerAddRequest.PhoneNumber);
        if (exists)
            throw new InvalidOperationException(
                _localizer["Customer.PhoneAlreadyExists", customerAddRequest.PhoneNumber]);

        Customer customer = _mapper.Map<Customer>(customerAddRequest);

        (customer.Name, customer.NameUr) = await _bilingual.ResolveAsync(customerAddRequest.Name);

        if (!string.IsNullOrWhiteSpace(customerAddRequest.Address))
            (customer.Address, customer.AddressUr) = await _bilingual.ResolveAsync(customerAddRequest.Address);

        Customer added = await _customerRepository.AddAsync(customer);

        return _mapper.Map<CustomerResponse>(added);
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        Customer? existing = await _customerRepository.GetByIdAsync(id);
        if (existing is null)
            throw new KeyNotFoundException(_localizer["Customer.NotFoundById", id]);

        return await _customerRepository.DeleteAsync(id);
    }

    public async Task<CustomerListResponse> GetAllCustomersAsync(Guid businessId)
    {
        var customers = await _customerRepository.GetByBusinessIdAsync(businessId);

        return new CustomerListResponse
        {
            Customers = _mapper.Map<List<CustomerResponse>>(customers),
            TotalCustomers = customers.Count,
            TotalOutstanding = customers.Sum(c => c.OutstandingBalance)
        };
    }

    public async Task<CustomerResponse?> GetCustomerByIdAsync(Guid id)
    {
        Customer? customer = await _customerRepository.GetByIdAsync(id);
        return customer is null ? null : _mapper.Map<CustomerResponse>(customer);
    }

    public async Task<CustomerListResponse?> GetCustomerByNameAsync(string name, Guid businessId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        List<Customer> all = await _customerRepository.GetByBusinessIdAsync(businessId);
        Customer? match = all.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            throw new KeyNotFoundException(_localizer["Customer.NotFoundByName", name]);

        var mapped = _mapper.Map<CustomerResponse>(match);
        return new CustomerListResponse { Customers = new List<CustomerResponse> { mapped } };
    }

    public async Task<PaginatedCustomerResponse> GetCustomersPagedAsync(
    Guid businessId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), _localizer["General.PageNumber.Invalid"]);

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), _localizer["General.PageSize.Invalid"]);

        var (items, totalCount) = await _customerRepository.GetPagedAsync(businessId, pageNumber, pageSize);
        var mapped = _mapper.Map<List<CustomerResponse>>(items);

        var allCustomers = await _customerRepository.GetByBusinessIdAsync(businessId);
        var totalOutstanding = allCustomers.Sum(c => c.OutstandingBalance);

        return new PaginatedCustomerResponse(mapped, pageNumber, pageSize, totalCount, totalOutstanding);
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(CustomerUpdateRequest customerUpdateRequest)
    {
        ArgumentNullException.ThrowIfNull(customerUpdateRequest);
        var validation = await _updateValidator.ValidateAsync(customerUpdateRequest);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Customer? existing = await _customerRepository.GetByIdAsync(customerUpdateRequest.Id);
        if (existing is null)
            throw new KeyNotFoundException(_localizer["Customer.NotFoundById", customerUpdateRequest.Id]);

        bool phoneChanged = !string.Equals(
            existing.PhoneNumber,
            customerUpdateRequest.PhoneNumber,
            StringComparison.Ordinal);

        if (phoneChanged && !string.IsNullOrWhiteSpace(customerUpdateRequest.PhoneNumber))
        {
            Customer? conflict = await _customerRepository.GetByPhoneAsync(existing.BusinessId, customerUpdateRequest.PhoneNumber);

            if (conflict is not null && conflict.Id != customerUpdateRequest.Id)
                throw new InvalidOperationException(
                    _localizer["Customer.PhoneInUseByAnother", customerUpdateRequest.PhoneNumber]);
        }

        bool nameChanged = _bilingual.ContainsUrduScript(customerUpdateRequest.Name)
            ? !string.Equals(existing.NameUr, customerUpdateRequest.Name, StringComparison.Ordinal)
            : !string.Equals(existing.Name, customerUpdateRequest.Name, StringComparison.Ordinal);

        bool addressChanged = _bilingual.ContainsUrduScript(customerUpdateRequest.Address)
            ? !string.Equals(existing.AddressUr, customerUpdateRequest.Address, StringComparison.Ordinal)
            : !string.Equals(existing.Address, customerUpdateRequest.Address, StringComparison.Ordinal);

        bool nameUrStale = _bilingual.IsTranslationStale(existing.Name, existing.NameUr);
        bool addressUrStale = !string.IsNullOrWhiteSpace(existing.Address)
            && _bilingual.IsTranslationStale(existing.Address, existing.AddressUr);

        _mapper.Map(customerUpdateRequest, existing);

        if (nameChanged || nameUrStale)
            (existing.Name, existing.NameUr) = await _bilingual.ResolveAsync(customerUpdateRequest.Name);

        if ((addressChanged || addressUrStale) && !string.IsNullOrWhiteSpace(customerUpdateRequest.Address))
            (existing.Address, existing.AddressUr) = await _bilingual.ResolveAsync(customerUpdateRequest.Address);

        Customer updated = await _customerRepository.UpdateAsync(existing);

        return _mapper.Map<CustomerResponse>(updated);
    }
}