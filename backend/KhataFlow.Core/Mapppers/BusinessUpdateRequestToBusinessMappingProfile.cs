using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO;

public class BusinessUpdateRequestToBusinessMappingProfile : Profile
{
    public BusinessUpdateRequestToBusinessMappingProfile()
    {
        CreateMap<BusinessUpdateRequest, Business>()
            .ForMember(dest => dest.BusinessName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address)) // now explicit
            .ForMember(dest => dest.SubscriptionPlan, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionExpiry, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerName, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerEmail, opt => opt.Ignore())
            .ForMember(dest => dest.Customers, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.Categories, opt => opt.Ignore())
            .ForMember(dest => dest.Sales, opt => opt.Ignore());
    }
}