using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mappers;

public class NotificationToNotificationResponseMappingProfile : Profile
{
    public NotificationToNotificationResponseMappingProfile()
    {
        CreateMap<Notification, NotificationResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.TitleUr, opt => opt.MapFrom(src => src.TitleUr))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.MessageUr, opt => opt.MapFrom(src => src.MessageUr))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead))
            .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.SentAt))
            .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId));
    }
}