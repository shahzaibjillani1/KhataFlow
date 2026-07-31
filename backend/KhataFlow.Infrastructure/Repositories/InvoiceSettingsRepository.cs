using Microsoft.EntityFrameworkCore;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Infrastructure.Data;

namespace KhataFlow.Infrastructure.Repositories;

public class InvoiceSettingsRepository : IInvoiceSettingsRepository
{
    private readonly AppDbContext _context;

    public InvoiceSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InvoiceSettings?> GetByBusinessIdAsync(Guid businessId)
    {
        return await _context.InvoiceSettings
            .Where(s => s.BusinessId == businessId && !s.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<InvoiceSettings> UpsertAsync(InvoiceSettings settings)
    {
        var existing = await _context.InvoiceSettings
            .FirstOrDefaultAsync(s => s.BusinessId == settings.BusinessId && !s.IsDeleted);

        if (existing is null)
        {
            settings.CreatedAt = DateTime.UtcNow;
            await _context.InvoiceSettings.AddAsync(settings);
            await _context.SaveChangesAsync();
            return settings;
        }

        existing.LogoUrl = settings.LogoUrl;
        existing.PrimaryColorHex = settings.PrimaryColorHex;
        existing.AccentColorHex = settings.AccentColorHex;
        existing.FooterNote = settings.FooterNote;
        existing.ShowBusinessAddress = settings.ShowBusinessAddress;
        existing.FontFamily = settings.FontFamily;
        existing.Style = settings.Style;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }
}