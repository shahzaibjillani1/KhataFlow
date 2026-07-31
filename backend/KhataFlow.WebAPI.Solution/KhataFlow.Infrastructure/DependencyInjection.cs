using KhataFlow.Core.Configuration;
using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.ServiceContracts;
using KhataFlow.Core.Services;
using KhataFlow.Infrastructure.Data;
using KhataFlow.Infrastructure.ExternalServices;
using KhataFlow.Infrastructure.ExternalServices.Gemini;
using KhataFlow.Infrastructure.ExternalServices.Groq;
using KhataFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace KhataFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("IdentityConnection"))
        );

        services.Configure<SafepayOptions>(configuration.GetSection("Safepay"));
        services.AddSingleton<SafepaySignatureVerifier>();
        services.AddScoped<ISubscriptionCheckoutService, SubscriptionCheckoutService>();
        services.AddScoped<IInvoiceSettingsRepository, InvoiceSettingsRepository>();
        services
            .AddHttpClient<IPaymentGatewayService, SafepayService>()
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { AllowAutoRedirect = false }
            );

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AppDbConnection"))
        );
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));
        services
            .AddHttpClient<GeminiClientService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(GetRetryPolicy());
        services.AddKeyedScoped<IAIClientService, GeminiClientService>("primary");

        services
            .AddHttpClient<GroqClientService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(GetRetryPolicy());
        services.AddKeyedScoped<IAIClientService, GroqClientService>("secondary");

        services.AddScoped<IAIClientService, FallbackAIClientService>();
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlatformReportRepository, PlatformReportRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250))
            );
}
