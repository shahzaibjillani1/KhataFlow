using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

public class BusinessToBusinessResponseMappingProfile : Profile
{
    public BusinessToBusinessResponseMappingProfile()
    {
        CreateMap<Business, BusinessResponse>()
            .ConstructUsing(src => new BusinessResponse(
                src.Id,
                src.BusinessName,
                src.BusinessNameUr,
                src.Email,
                src.PhoneNumber,
                src.Address ?? "",
                src.AddressUr,
                src.Status,
                src.SubscriptionPlan,
                src.SubscriptionExpiry,
                src.CreatedAt
            ));
    }
}