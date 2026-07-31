using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Request;

namespace KhataFlow.Core.Mapppers;

public class ProductToProductUpdateRequestMappingProfile: Profile
{
    public ProductToProductUpdateRequestMappingProfile()
    {
        CreateMap<ProductUpdateRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
            .ForMember(dest => dest.InventoryStatus, opt => opt.Ignore())   // computed property
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.BusinessId, opt => opt.Ignore())
            .ForMember(dest => dest.Business, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.SaleItems, opt => opt.Ignore());
    }
}
