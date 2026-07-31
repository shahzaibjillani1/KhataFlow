using System.Globalization;
using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.Enums;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace KhataFlow.Core.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private static readonly string BrandPurple = "#5B4FE9";
    private static readonly string LightGreen = "#E8F8EF";
    private static readonly string GreenText = "#1E9E5A";

    public InvoiceService(ISaleRepository saleRepository, IStringLocalizer<SharedResource> localizer)
    {
        _saleRepository = saleRepository;
        _localizer = localizer;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid businessId, Guid saleId)
    {
        Sale sale = await _saleRepository.GetByIdAsync(businessId, saleId)
            ?? throw new KeyNotFoundException(_localizer["Invoice.Sale.NotFound", saleId]);

        bool isUrdu = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ur";

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.ContentFromRightToLeft(); // no-op if QuestPDF version lacks this; see note below

                page.Content().Column(col =>
                {
                    col.Item().Background(BrandPurple).Padding(20).Column(header =>
                    {
                        header.Item().Text(DisplayName(sale.Business.BusinessName, sale.Business.BusinessNameUr, isUrdu))
                            .FontSize(18).Bold().FontColor(Colors.White);

                        header.Item().PaddingTop(2).Text(BuildBusinessSubtitle(sale.Business, isUrdu))
                            .FontSize(9).FontColor(Colors.White);

                        header.Item().PaddingTop(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(_localizer["Invoice.Label.InvoiceNumber"]).FontSize(7).FontColor(Colors.Grey.Lighten2);
                                c.Item().Text(sale.InvoiceNumber).FontSize(11).Bold().FontColor(Colors.White);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(_localizer["Invoice.Label.Date"]).FontSize(7).FontColor(Colors.Grey.Lighten2);
                                c.Item().Text(sale.Date.ToString("dd MMM yyyy, h:mm tt")).FontSize(11).Bold().FontColor(Colors.White);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(_localizer["Invoice.Label.Customer"]).FontSize(7).FontColor(Colors.Grey.Lighten2);
                                c.Item().Text(DisplayName(
                                    sale.Customer?.Name ?? _localizer["Invoice.WalkInCustomer"],
                                    sale.Customer?.NameUr,
                                    isUrdu)).FontSize(11).Bold().FontColor(Colors.White);
                                if (!string.IsNullOrWhiteSpace(sale.Customer?.PhoneNumber))
                                    c.Item().Text(sale.Customer.PhoneNumber).FontSize(8).FontColor(Colors.Grey.Lighten2);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(_localizer["Invoice.Label.Status"]).FontSize(7).FontColor(Colors.Grey.Lighten2);
                                c.Item().Text(LocalizeEnum(sale.PaymentStatus)).FontSize(11).Bold().FontColor(Colors.White);
                                c.Item().Text(LocalizeEnum(sale.PaymentMethod)).FontSize(8).FontColor(Colors.Grey.Lighten2);
                            });
                        });
                    });

                    col.Item().Padding(20).Column(body =>
                    {
                        body.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(20);
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.5f);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text(_localizer["Invoice.Column.Number"]).FontSize(8).FontColor(Colors.Grey.Medium);
                                h.Cell().Text(_localizer["Invoice.Column.Item"]).FontSize(8).FontColor(Colors.Grey.Medium);
                                h.Cell().AlignCenter().Text(_localizer["Invoice.Column.Qty"]).FontSize(8).FontColor(Colors.Grey.Medium);
                                h.Cell().AlignRight().Text(_localizer["Invoice.Column.UnitPrice"]).FontSize(8).FontColor(Colors.Grey.Medium);
                                h.Cell().AlignRight().Text(_localizer["Invoice.Column.Total"]).FontSize(8).FontColor(Colors.Grey.Medium);
                            });

                            int i = 1;
                            foreach (var item in sale.Items)
                            {
                                table.Cell().PaddingTop(8).Text(i.ToString());
                                table.Cell().PaddingTop(8).Text(DisplayName(item.Product.ProductName, item.Product.ProductNameUr, isUrdu)).Bold();
                                table.Cell().PaddingTop(8).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().PaddingTop(8).AlignRight().Text($"Rs {item.UnitPrice:N0}");
                                table.Cell().PaddingTop(8).AlignRight().Text($"Rs {item.Total:N0}").Bold();
                                i++;
                            }
                        });

                        body.Item().PaddingTop(16).AlignRight().Column(totals =>
                        {
                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text(_localizer["Invoice.Label.Subtotal"]).FontColor(Colors.Grey.Medium);
                                r.ConstantItem(90).AlignRight().Text($"Rs {sale.Subtotal:N0}");
                            });

                            if (sale.DiscountAmount > 0)
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().AlignRight().Text(_localizer["Invoice.Label.Discount"]).FontColor(Colors.Grey.Medium);
                                    r.ConstantItem(90).AlignRight().Text($"- Rs {sale.DiscountAmount:N0}");
                                });
                            }

                            totals.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text(_localizer["Invoice.Label.GrandTotal"]).FontSize(13).Bold().FontColor(BrandPurple);
                                r.ConstantItem(90).AlignRight().Text($"Rs {sale.GrandTotal:N0}").FontSize(13).Bold().FontColor(BrandPurple);
                            });

                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text(_localizer["Invoice.Label.Payment"]).FontColor(Colors.Grey.Medium);
                                r.ConstantItem(90).AlignRight().Text(LocalizeEnum(sale.PaymentMethod));
                            });
                        });

                        if (sale.PaymentStatus == PaymentStatus.Paid)
                        {
                            body.Item().PaddingTop(14).Background(LightGreen).Padding(10).Text(text =>
                            {
                                text.Span("✓ ").FontColor(GreenText);
                                text.Span(_localizer["Invoice.PaymentReceived.Prefix"]).FontColor(GreenText);
                                text.Span($" Rs {sale.GrandTotal:N0} ").Bold().FontColor(GreenText);
                                text.Span(string.Format(_localizer["Invoice.PaymentReceived.Suffix"], LocalizeEnum(sale.PaymentMethod))).FontColor(GreenText);
                            });
                        }

                        body.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Text(_localizer["Invoice.ThankYou"]).FontSize(9).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span(_localizer["Invoice.PoweredBy"]).FontSize(8).FontColor(Colors.Grey.Medium);
                                text.Span("KhataFlow").FontSize(8).Bold();
                            });
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private string DisplayName(string englishName, string? urduName, bool isUrdu) =>
        isUrdu && !string.IsNullOrWhiteSpace(urduName) ? urduName : englishName;

    private string BuildBusinessSubtitle(Business business, bool isUrdu)
    {
        var parts = new List<string>();
        var address = isUrdu && !string.IsNullOrWhiteSpace(business.AddressUr) ? business.AddressUr : business.Address;
        if (!string.IsNullOrWhiteSpace(address)) parts.Add(address);
        if (!string.IsNullOrWhiteSpace(business.PhoneNumber)) parts.Add(business.PhoneNumber);
        return string.Join(" • ", parts);
    }

    private string LocalizeEnum(Enum value) =>
        _localizer[$"Enum.{value.GetType().Name}.{value}"].Value is var v && !string.IsNullOrEmpty(v)
            ? v
            : value.ToString();
}