using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string PlanName { get; set; } = string.Empty;
    public string PlanNameUr { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public ICollection<string> Features { get; set; } = new List<string>();
    public ICollection<string> FeaturesUr { get; set; } = new List<string>();

    public SubscriptionPlanType PlanType { get; set; }     
    public bool IsActive { get; set; } = true;
    public int MaxProducts { get; set; }        
    public int MaxCustomers { get; set; }
    public int MaxStaffUsers { get; set; }
    public int MaxSalesPerMonth { get; set; }
    public bool AllowVoiceInput { get; set; }
    public bool AllowWhatsAppShare { get; set; }
    public bool AllowCustomBranding { get; set; }

}