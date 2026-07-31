using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mappers;

public class LedgerEntryToLedgerEntryResponseMappingProfile : Profile
{
    public LedgerEntryToLedgerEntryResponseMappingProfile()
    {
        CreateMap<LedgerEntry, LedgerEntryResponse>()

            .ForMember(
                dest => dest.Type,
                opt => opt.MapFrom(src => src.EntryType.ToString())
            )

            .ForMember(
                dest => dest.RunningBalance,
                opt => opt.Ignore()
            )

            .ForMember(
                dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreatedAt)
            )

            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(src => src.Id)
            )

            .ForMember(
                dest => dest.Amount,
                opt => opt.MapFrom(src => src.Amount)
            )

            .ForMember(
                dest => dest.Notes,
                opt => opt.MapFrom(src => src.Notes)
            );
    }
}