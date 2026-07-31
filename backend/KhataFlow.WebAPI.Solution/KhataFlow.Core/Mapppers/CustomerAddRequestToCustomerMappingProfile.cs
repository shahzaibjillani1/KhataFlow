using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO;

namespace KhataFlow.Core.Mapppers;

public class CustomerAddRequestToCustomerMappingProfile: Profile
{
    public CustomerAddRequestToCustomerMappingProfile()
    {
        CreateMap<CustomerAddRequest, Customer>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Address, opt => opt.Ignore())
            .ForMember(dest => dest.LastVisit, opt => opt.MapFrom(src => src.LastVisit))
            .ForMember(dest => dest.Business, opt => opt.Ignore())
            .ForMember(dest => dest.Sales, opt => opt.Ignore())
            .ForMember(dest => dest.LedgerEntries, opt => opt.Ignore());
    }
}
