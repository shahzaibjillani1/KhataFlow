using Asp.Versioning;
using KhataFlow.Core;
using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Infrastructure;
using KhataFlow.Infrastructure.Data;
using KhataFlow.Infrastructure.Hubs;
using KhataFlow.WebAPI;
using KhataFlow.WebAPI.Extensions;
using KhataFlow.WebAPI.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCoreServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices();

builder.Services.AddLocalization();

var supportedCultures = new[] { "en", "ur" };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options
        .SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

builder
    .Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddConfiguredAuthentication(builder.Configuration);
builder.Services.AddConfiguredSwagger();
builder.Services.AddConfiguredCors(builder.Configuration);
builder.Services.AddConfiguredSignalR();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var appDb = services.GetRequiredService<AppDbContext>();
    var identityDb = services.GetRequiredService<ApplicationDbContext>();

    await identityDb.Database.MigrateAsync();
    await appDb.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var config = services.GetRequiredService<IConfiguration>();

    await AdminUserSeeder.SeedAsync(roleManager, userManager, config);
    await SubscriptionPlanSeeder.SeedAsync(appDb);
}

app.UseRequestLocalization();

app.UseExceptionHandlingMiddleware();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
