using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mapppers;

public class CategoryToCategoryResponseMappingProfile : Profile
{
    public CategoryToCategoryResponseMappingProfile()
    {
        CreateMap<Category, CategoryResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName))
            .ForMember(dest => dest.CategoryNameUr, opt => opt.MapFrom(src => src.CategoryNameUr));
    }
}