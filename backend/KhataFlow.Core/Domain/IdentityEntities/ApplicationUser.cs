using KhataFlow.Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace KhataFlow.Core.Domain.IdentityEntities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public string? FullNameUr { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNameUr { get; set; }  
    public string? ProfilePictureUrl { get; set; }
    public Guid BusinessId { get; set; }
    public Gender Gender { get; set; } = Gender.PreferNotToSay;
    public DateTime? DateOfBirth { get; set; }
    public UserRole Role { get; set; } = UserRole.Owner;
    public AccountStatus Status { get; set; } = AccountStatus.PendingVerification;
    public SubscriptionPlanType Plan { get; set; } = SubscriptionPlanType.Free;
    public DateTime? PlanExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}