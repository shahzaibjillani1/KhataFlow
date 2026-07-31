using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Enums;

namespace KhataFlow.Core.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUr { get; set; }   
    public string? Address { get; set; }
    public string? AddressUr { get; set; }  
    public string? PhoneNumber { get; set; }

    public DateTime? LastVisit { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    public decimal TotalPurchases => Sales?.Sum(s => s.TotalAmount) ?? 0;
    public decimal OutstandingBalance => LedgerEntries?.Sum(e =>
        e.EntryType == LedgerEntryType.Udhar ? e.Amount : -e.Amount) ?? 0;
    public string PublicToken { get; set; } = GenerateToken();

    private static string GenerateToken()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        var sb = new System.Text.StringBuilder(8);
        foreach (var b in buffer)
            sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }
}