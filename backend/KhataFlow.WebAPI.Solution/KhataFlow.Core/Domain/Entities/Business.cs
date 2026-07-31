using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Exceptions;

namespace KhataFlow.Core.Domain.Entities;

public class Business : BaseEntity
{
    public Guid OwnerId { get; set; }

    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessNameUr { get; set; }  
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerNameUr { get; set; }     
    public string OwnerEmail { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? SuspensionReason { get; set; }
    public string? SuspensionReasonUr { get; set; }
    public string? Address { get; set; }
    public string? AddressUr { get; set; }

    public SubscriptionPlanType SubscriptionPlan { get; set; } = SubscriptionPlanType.Free;
    public BusinessStatus Status { get; set; }
    public DateTime SubscriptionExpiry { get; set; }   
    public DateTime? SubscriptionRenewsAt { get; set; }         

    public bool IsPremiumActive =>
        SubscriptionPlan == SubscriptionPlanType.Premium
        && SubscriptionRenewsAt.HasValue
        && SubscriptionRenewsAt.Value > DateTime.UtcNow;

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public void Suspend(string reason, string? reasonUr = null)
    {
        if (Status == BusinessStatus.Suspended)
            throw new DomainException("Business is already suspended.");

        Status = BusinessStatus.Suspended;
        SuspensionReason = reason;
        SuspensionReasonUr = reasonUr;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status == BusinessStatus.Active)
            throw new DomainException("Business is already active.");

        Status = BusinessStatus.Active;
        SuspensionReason = null;
        SuspensionReasonUr = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RenewSubscription(int days)
    {
        SubscriptionExpiry = DateTime.UtcNow.AddDays(days);
        Status = BusinessStatus.Active;
        SuspensionReason = null;
        SuspensionReasonUr = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsSubscriptionExpired()
        => DateTime.UtcNow > SubscriptionExpiry;
}