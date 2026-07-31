using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.ServiceContracts;

namespace KhataFlow.Core.Services;

public class ReportService : IReportService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IExpenseRepository _expenseRepository; // new
    private readonly ICustomerRepository customerRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<DateRange> _dateRangeValidator;

    public ReportService(
        ISaleRepository saleRepository,
        ILedgerRepository ledgerRepository,
        IExpenseRepository expenseRepository,
        ICustomerRepository customerRepository,
        IMapper mapper,
        IValidator<DateRange> dateRangeValidator)
    {
        _saleRepository = saleRepository;
        _ledgerRepository = ledgerRepository;
        _expenseRepository = expenseRepository;
        this.customerRepository = customerRepository;
        _mapper = mapper;
        _dateRangeValidator = dateRangeValidator;
    }

    public async Task<FinancialSummaryResponse> GetFinancialSummaryAsync(Guid businessId, DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        await ValidateDateRangeAsync(range);

        decimal totalRevenue = await _saleRepository.GetTotalRevenueAsync(businessId, range.From, range.To);
        int totalOrders = await _saleRepository.GetSaleCountAsync(businessId);
        int totalCustomers = await customerRepository.GetCustomerCountAsync(businessId);
        List<LedgerEntry> ledgerEntries = await _ledgerRepository.GetByBusinessIdAsync(businessId);

        decimal totalExpenses = await _expenseRepository.GetTotalInRangeAsync(businessId, range.From, range.To);
        decimal totalOutstanding = CalculateOutstanding(ledgerEntries);

        var builder = new FinancialSummaryBuilder(
            From: range.From,
            To: range.To,
            TotalRevenue: totalRevenue,
            TotalExpenses: totalExpenses,
            TotalOutstanding: totalOutstanding,
            TotalOrders: totalOrders,
            TotalCustomers: totalCustomers);

        return _mapper.Map<FinancialSummaryResponse>(builder);
    }

    public async Task<decimal> GetGrossProfitAsync(Guid businessId, DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        await ValidateDateRangeAsync(range);

        decimal totalRevenue = await _saleRepository.GetTotalRevenueAsync(businessId, range.From, range.To);
        decimal totalExpenses = await _expenseRepository.GetTotalInRangeAsync(businessId, range.From, range.To);

        return totalRevenue - totalExpenses;
    }

    public async Task<decimal> GetTotalExpensesAsync(Guid businessId, DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        await ValidateDateRangeAsync(range);

        return await _expenseRepository.GetTotalInRangeAsync(businessId, range.From, range.To);
    }

    private static decimal CalculateOutstanding(List<LedgerEntry> entries)
        => entries.Sum(e => e.EntryType == LedgerEntryType.Udhar ? e.Amount : -e.Amount);

    private async Task ValidateDateRangeAsync(DateRange range)
    {
        var validation = await _dateRangeValidator.ValidateAsync(range);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
    }
}

