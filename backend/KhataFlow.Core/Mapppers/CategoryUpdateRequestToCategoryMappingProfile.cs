using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO;

namespace KhataFlow.Core.Mapppers;

public class CategoryUpdateRequestToCategoryMappingProfile: Profile
{
    public CategoryUpdateRequestToCategoryMappingProfile()
    {
        CreateMap<CategoryUpdateRequest, Category>().ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)).ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName));
    }
}
