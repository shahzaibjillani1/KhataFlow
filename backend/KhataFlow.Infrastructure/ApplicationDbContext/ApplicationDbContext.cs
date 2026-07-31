using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(150);
            e.Property(u => u.FullNameUr).HasMaxLength(150);
            e.Property(u => u.DisplayName).HasMaxLength(100);
            e.Property(u => u.DisplayNameUr).HasMaxLength(100);
        });

        builder.Entity<UserRefreshToken>(e =>
        {
            e.ToTable("UserRefreshTokens", "identity");
            e.HasKey(t => t.Id);
            e.Property(t => t.Token).IsRequired().HasMaxLength(500);
            e.Property(t => t.JwtId).IsRequired().HasMaxLength(100);

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.Token).HasDatabaseName("IX_UserRefreshToken_Token");
            e.HasIndex(t => t.UserId).HasDatabaseName("IX_UserRefreshToken_UserId");
            e.HasQueryFilter(t => !t.IsRevoked);
        });
    }

    #region SaveChanges (Audit + Soft Delete)

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var entries = ChangeTracker.Entries<ApplicationUser>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }
    }

    #endregion

    #region Seeding

    private static void SeedAdminData(ModelBuilder builder)
    {
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var adminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        builder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Id = adminRoleId,
            Name = "Admin",
            NormalizedName = "ADMIN",
            ConcurrencyStamp = "cccccccc-cccc-cccc-cccc-cccccccccccc"
        });

        builder.Entity<ApplicationUser>().HasData(new ApplicationUser
        {
            Id = adminUserId,
            UserName = "admin@khataflow.com",
            NormalizedUserName = "ADMIN@KHATAFLOW.COM",
            Email = "admin@khataflow.com",
            NormalizedEmail = "ADMIN@KHATAFLOW.COM",
            EmailConfirmed = true,
            FullName = "System Admin",
            Role = UserRole.SuperAdmin,
            Status = AccountStatus.Active,
            SecurityStamp = "ssssssss-ssss-ssss-ssss-ssssssssssss",
            ConcurrencyStamp = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            LockoutEnabled = false,
            AccessFailedCount = 0,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PasswordHash = "AQAAAAIAAYagAAAAEGL49UDndwoIi6GKuL3AIO4BUswW9GF+M5nrgjlo2DlYK3/amvy9yNCH1wXUWNoHWA=="
        });

        builder.Entity<IdentityUserRole<Guid>>().HasData(new IdentityUserRole<Guid>
        {
            UserId = adminUserId,
            RoleId = adminRoleId
        });
    }

    #endregion
}