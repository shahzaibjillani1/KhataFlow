using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Mappers;

public class RecordPaymentRequestToLedgerEntryMappingProfile : Profile
{
    public RecordPaymentRequestToLedgerEntryMappingProfile()
    {
        CreateMap<RecordPaymentRequest, LedgerEntry>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes));
            
    }
}