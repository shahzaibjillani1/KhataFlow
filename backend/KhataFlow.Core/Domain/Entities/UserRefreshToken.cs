using KhataFlow.Core.Domain.Common;

namespace KhataFlow.Core.Domain.IdentityEntities;

public class UserRefreshToken : BaseEntity
{
    public Guid UserId { get; set; }       
    public string Token { get; set; } = string.Empty;    
    public string JwtId { get; set; } = string.Empty;   
    public bool IsRevoked { get; set; } = false;
    public bool IsUsed { get; set; } = false;
    public DateTime ExpiresAt { get; set; }

    public ApplicationUser? User { get; set; }
}