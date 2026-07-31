using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mappers;

public class SaleToSaleResponseMappingProfile : Profile
{
    public SaleToSaleResponseMappingProfile()
    {
        CreateMap<SaleItem, SaleItemResponse>()
            .ForCtorParam(
                nameof(SaleItemResponse.ProductId),
                opt => opt.MapFrom(src => src.ProductId)
            )
            .ForCtorParam(
                nameof(SaleItemResponse.ProductName),
                opt =>
                    opt.MapFrom(src =>
                        src.Product != null ? src.Product.ProductName : "Unknown Product"
                    )
            )
            .ForCtorParam(
                nameof(SaleItemResponse.ProductNameUr),
                opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductNameUr : null)
            )
            .ForCtorParam(
                nameof(SaleItemResponse.Quantity),
                opt => opt.MapFrom(src => src.Quantity)
            )
            .ForCtorParam(
                nameof(SaleItemResponse.UnitPrice),
                opt => opt.MapFrom(src => src.UnitPrice)
            )
            .ForCtorParam(nameof(SaleItemResponse.LineTotal), opt => opt.MapFrom(src => src.Total));

        CreateMap<Sale, SaleResponse>()
            .ForCtorParam(nameof(SaleResponse.Id), opt => opt.MapFrom(src => src.Id))
            .ForCtorParam(
                nameof(SaleResponse.InvoiceNumber),
                opt => opt.MapFrom(src => src.InvoiceNumber)
            )
            .ForCtorParam(nameof(SaleResponse.Date), opt => opt.MapFrom(src => src.Date))
            .ForCtorParam(
                nameof(SaleResponse.CustomerName),
                opt =>
                    opt.MapFrom(src =>
                        src.Customer != null ? src.Customer.Name : "Walk-in Customer"
                    )
            )
            .ForCtorParam(
                nameof(SaleResponse.CustomerNameUr),
                opt =>
                    opt.MapFrom(src => src.Customer != null ? src.Customer.NameUr : "واک ان کسٹمر")
            )
            .ForCtorParam(
                nameof(SaleResponse.TotalAmount),
                opt => opt.MapFrom(src => src.TotalAmount)
            )
            .ForCtorParam(
                nameof(SaleResponse.ItemCount),
                opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0)
            )
            .ForCtorParam(
                nameof(SaleResponse.PaymentStatus),
                opt => opt.MapFrom(src => src.PaymentStatus)
            )
            .ForCtorParam(
                nameof(SaleResponse.Items),
                opt => opt.MapFrom(src => src.Items ?? new List<SaleItem>())
            );
    }
}
