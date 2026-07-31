using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO;

namespace KhataFlow.Core.Mapppers;

public class CategoryAddRequestToCategoryMappingProfile: Profile
{
    public CategoryAddRequestToCategoryMappingProfile()
    {
        CreateMap<CategoryAddRequest, Category>().ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName));
    }
}
