using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Data;

public static class SubscriptionPlanSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.SubscriptionPlans.AnyAsync())
            return;

        context.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanName = "Free",
                PlanNameUr = "مفت",
                MonthlyPrice = 0,
                PlanType = SubscriptionPlanType.Free,
                IsActive = true,
                MaxProducts = 50,
                MaxCustomers = 100,
                MaxStaffUsers = 1,
                MaxSalesPerMonth = 150,
                AllowVoiceInput = false,
                AllowWhatsAppShare = false,
                AllowCustomBranding = false,
                Features = new List<string>
                {
                    "Up to 50 products",
                    "Up to 100 customers",
                    "Manual sales entry (POS)",
                    "Udhar (credit) tracking",
                    "Manual expense entry",
                    "Invoice with print & download",
                    "30 days sales & udhar history",
                    "1 staff login (owner only)"
                },
                FeaturesUr = new List<string>()
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanName = "Premium",
                PlanNameUr = "پریمیم",
                MonthlyPrice = 999,
                PlanType = SubscriptionPlanType.Premium,
                IsActive = true,
                MaxProducts = -1,
                MaxCustomers = -1,
                MaxStaffUsers = 3,
                MaxSalesPerMonth = -1,
                AllowVoiceInput = true,
                AllowWhatsAppShare = true,
                AllowCustomBranding = true,
                Features = new List<string>
                {
                    "Unlimited products & customers",
                    "Voice-based sale entry",
                    "Voice-based udhar & expense entry",
                    "WhatsApp ledger sharing with customers",
                    "Full reports & CSV/PDF/Excel export",
                    "Unlimited sales & udhar history",
                    "Up to 3 staff logins with roles",
                    "Remove KhataFlow branding from invoice",
                    "Priority WhatsApp support"
                },
                FeaturesUr = new List<string>()
            }
        );

        await context.SaveChangesAsync();
    }
}