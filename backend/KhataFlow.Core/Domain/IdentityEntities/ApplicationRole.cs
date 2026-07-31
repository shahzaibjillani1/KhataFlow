using Microsoft.AspNetCore.Identity;

namespace KhataFlow.Core.Domain.IdentityEntities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}