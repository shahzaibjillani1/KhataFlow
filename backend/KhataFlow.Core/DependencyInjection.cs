using FluentValidation;
using KhataFlow.Core.Configuration;
using KhataFlow.Core.ServiceContracts;
using KhataFlow.Core.Services;
using KhataFlow.Core.Validators;
using KhataFlow.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KhataFlow.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));

        services.Scan(s => s.FromAssemblyOf<CustomerService>()
            .AddClasses(c => c.Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        services.AddScoped<VoiceIntentExtractor>();
        services.AddValidatorsFromAssemblyContaining<CustomerAddRequestValidator>();
        services.AddScoped<IInvoiceDocumentBuilder, QuestPdfInvoiceDocumentBuilder>();

        return services;
    }
}