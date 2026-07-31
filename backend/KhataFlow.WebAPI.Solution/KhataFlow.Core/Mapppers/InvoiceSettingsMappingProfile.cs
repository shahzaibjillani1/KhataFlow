using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mapping;

public class InvoiceSettingsMappingProfile : Profile
{
    public InvoiceSettingsMappingProfile()
    {
        CreateMap<InvoiceSettingsRequest, InvoiceSettings>();
        CreateMap<InvoiceSettings, InvoiceSettingsResponse>();
    }
}