using AutoMapper;
using FluentValidation;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.ServiceContracts;

namespace KhataFlow.Core.Services;

public class InvoiceSettingsService : IInvoiceSettingsService
{
    private readonly IInvoiceSettingsRepository _repository;
    private readonly IPlanLimitService _planLimitService;
    private readonly IMapper _mapper;
    private readonly IValidator<InvoiceSettingsRequest> _validator;

    public InvoiceSettingsService(
        IInvoiceSettingsRepository repository,
        IPlanLimitService planLimitService,
        IMapper mapper,
        IValidator<InvoiceSettingsRequest> validator)
    {
        _repository = repository;
        _planLimitService = planLimitService;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<InvoiceSettingsResponse> GetAsync(Guid businessId)
    {
        var settings = await _repository.GetByBusinessIdAsync(businessId);
        settings ??= new InvoiceSettings { BusinessId = businessId };

        return _mapper.Map<InvoiceSettingsResponse>(settings);
    }

    public async Task<InvoiceSettingsResponse> UpdateAsync(InvoiceSettingsRequest request, Guid businessId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _planLimitService.EnsureFeatureEnabledAsync(businessId, PlanFeature.CustomBranding);

        var settings = _mapper.Map<InvoiceSettings>(request);
        settings.BusinessId = businessId;

        var saved = await _repository.UpsertAsync(settings);
        return _mapper.Map<InvoiceSettingsResponse>(saved);
    }
}