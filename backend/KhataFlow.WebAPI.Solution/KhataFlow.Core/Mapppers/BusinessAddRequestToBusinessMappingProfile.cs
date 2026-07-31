using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO;

public class BusinessAddRequestToBusinessMappingProfile : Profile
{
    public BusinessAddRequestToBusinessMappingProfile()
    {
        CreateMap<BusinessAddRequest, Business>()
            .ForMember(dest => dest.BusinessName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.OwnerEmail))
            .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.OwnerName))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.address))
            .ForMember(dest => dest.SubscriptionPlan, opt => opt.MapFrom(src => src.Plan))
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.phoneNumber))
            .ForMember(dest => dest.SubscriptionExpiry, opt => opt.Ignore())
            .ForMember(dest => dest.Customers, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.Categories, opt => opt.Ignore())
            .ForMember(dest => dest.Sales, opt => opt.Ignore());
    }
}