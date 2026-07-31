using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Exceptions;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;

namespace KhataFlow.Core.Services;

public class PlanLimitService : IPlanLimitService
{
    private readonly IBusinessRepository _businessRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PlanLimitService(
        IBusinessRepository businessRepository,
        ISubscriptionPlanRepository planRepository,
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IStringLocalizer<SharedResource> localizer)
    {
        _businessRepository = businessRepository;
        _planRepository = planRepository;
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task EnsureCanCreateSaleAsync(Guid businessId)
    {
        var plan = await GetPlanAsync(businessId);
        if (plan.MaxSalesPerMonth < 0) return;

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await _saleRepository.CountSinceAsync(businessId, monthStart);

        if (count >= plan.MaxSalesPerMonth)
            throw new PlanLimitExceededException(
                _localizer["Plan.Limit.SalesExceeded", plan.MaxSalesPerMonth], "MaxSalesPerMonth");
    }

    public async Task EnsureCanAddProductAsync(Guid businessId)
    {
        var plan = await GetPlanAsync(businessId);
        if (plan.MaxProducts < 0) return;

        var count = await _productRepository.CountByBusinessAsync(businessId);
        if (count >= plan.MaxProducts)
            throw new PlanLimitExceededException(
                _localizer["Plan.Limit.ProductsExceeded", plan.MaxProducts], "MaxProducts");
    }

    public async Task EnsureCanAddCustomerAsync(Guid businessId)
    {
        var plan = await GetPlanAsync(businessId);
        if (plan.MaxCustomers < 0) return;

        var count = await _customerRepository.CountByBusinessAsync(businessId);
        if (count >= plan.MaxCustomers)
            throw new PlanLimitExceededException(
                _localizer["Plan.Limit.CustomersExceeded", plan.MaxCustomers], "MaxCustomers");
    }

    public async Task EnsureCanAddStaffAsync(Guid businessId)
    {
        var plan = await GetPlanAsync(businessId);
        if (plan.MaxStaffUsers < 0) return;

        var count = await _userRepository.CountByBusinessAsync(businessId);
        if (count >= plan.MaxStaffUsers)
            throw new PlanLimitExceededException(
                _localizer["Plan.Limit.StaffExceeded", plan.MaxStaffUsers], "MaxStaffUsers");
    }

    public async Task EnsureFeatureEnabledAsync(Guid businessId, PlanFeature feature)
    {
        var plan = await GetPlanAsync(businessId);
        var allowed = feature switch
        {
            PlanFeature.VoiceInput => plan.AllowVoiceInput,
            PlanFeature.WhatsAppShare => plan.AllowWhatsAppShare,
            PlanFeature.CustomBranding => plan.AllowCustomBranding,
            _ => false
        };

        if (!allowed)
            throw new PlanLimitExceededException(
                _localizer["Plan.Limit.FeatureLocked", feature.ToString()], feature.ToString());
    }

    private async Task<Domain.Entities.SubscriptionPlan> GetPlanAsync(Guid businessId)
    {
        var business = await _businessRepository.GetByIdAsync(businessId)
            ?? throw new NotFoundException(_localizer["Business.NotFound", businessId]);

        return await _planRepository.GetByPlanTypeAsync(business.SubscriptionPlan)
            ?? throw new DomainException("Subscription plan configuration missing for this business.");
    }
}