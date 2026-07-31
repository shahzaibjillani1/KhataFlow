using AutoMapper;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mapppers;

public class ExpenseToExpenseResponseMappingProfile : Profile
{
    public ExpenseToExpenseResponseMappingProfile()
    {
        CreateMap<Expense, ExpenseResponse>();
    }
}