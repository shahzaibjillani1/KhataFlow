using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Mappers;

public class AddKhataRequestToLedgerEntryMappingProfile : Profile
{
    public AddKhataRequestToLedgerEntryMappingProfile()
    {
        CreateMap<AddUdharRequest, LedgerEntry>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.Date, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.EntryType, opt => opt.MapFrom(_ => LedgerEntryType.Udhar))
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.Business, opt => opt.Ignore())
            .ForMember(dest => dest.BusinessId, opt => opt.Ignore());
    }
}