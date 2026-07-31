using AutoMapper;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mappers;

public class FinancialSummaryBuilderToResponseMappingProfile : Profile
{
    public FinancialSummaryBuilderToResponseMappingProfile()
    {
        CreateMap<FinancialSummaryBuilder, FinancialSummaryResponse>()
            .ForCtorParam(nameof(FinancialSummaryResponse.From),
                opt => opt.MapFrom(src => src.From))
            .ForCtorParam(nameof(FinancialSummaryResponse.To),
                opt => opt.MapFrom(src => src.To))
            .ForCtorParam(nameof(FinancialSummaryResponse.TotalRevenue),
                opt => opt.MapFrom(src => src.TotalRevenue))
            .ForCtorParam(nameof(FinancialSummaryResponse.TotalExpenses),
                opt => opt.MapFrom(src => src.TotalExpenses))
            .ForCtorParam(nameof(FinancialSummaryResponse.GrossProfit),
                opt => opt.MapFrom(src => src.TotalRevenue - src.TotalExpenses))
            .ForCtorParam(nameof(FinancialSummaryResponse.TotalOutstanding),
                opt => opt.MapFrom(src => src.TotalOutstanding))
            .ForCtorParam(nameof(FinancialSummaryResponse.TotalOrders),
                opt => opt.MapFrom(src => src.TotalOrders))
            .ForCtorParam(nameof(FinancialSummaryResponse.TotalCustomers),
                opt => opt.MapFrom(src => src.TotalCustomers))
            .ForCtorParam(nameof(FinancialSummaryResponse.AverageOrderValue),
                opt => opt.MapFrom(src =>
                    src.TotalOrders > 0
                        ? Math.Round(src.TotalRevenue / src.TotalOrders, 2)
                        : 0));
    }
}