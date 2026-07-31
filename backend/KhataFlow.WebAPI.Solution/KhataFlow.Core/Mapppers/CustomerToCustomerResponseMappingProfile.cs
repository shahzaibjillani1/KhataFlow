using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mapppers;

public class CustomerToCustomerResponseMappingProfile : Profile
{
    public CustomerToCustomerResponseMappingProfile()
    {
        CreateMap<Customer, CustomerResponse>()
            .ForCtorParam(nameof(CustomerResponse.Name), opt => opt.MapFrom(src => src.Name))
            .ForCtorParam(nameof(CustomerResponse.NameUr), opt => opt.MapFrom(src => src.NameUr))
            .ForCtorParam(nameof(CustomerResponse.PhoneNumber), opt => opt.MapFrom(src => src.PhoneNumber))
            .ForCtorParam(nameof(CustomerResponse.Address), opt => opt.MapFrom(src => src.Address))
            .ForCtorParam(nameof(CustomerResponse.AddressUr), opt => opt.MapFrom(src => src.AddressUr))
            .ForCtorParam(nameof(CustomerResponse.LastVisit), opt => opt.MapFrom(src => src.LastVisit))
            .ForCtorParam(nameof(CustomerResponse.TotalPurchases), opt => opt.MapFrom(src => src.TotalPurchases))
            .ForCtorParam(nameof(CustomerResponse.UdharAmount), opt => opt.MapFrom(src => src.OutstandingBalance));
    }
}