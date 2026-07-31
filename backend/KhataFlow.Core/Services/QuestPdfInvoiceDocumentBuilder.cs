using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.ServiceContracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KhataFlow.Infrastructure.Documents;

public class QuestPdfInvoiceDocumentBuilder : IInvoiceDocumentBuilder
{
    public byte[] Build(Sale sale, InvoiceSettings settings, Business business)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily(settings.FontFamily));

                page.Header().Background(settings.AccentColorHex).Padding(15).Row(row =>
                {
                    if (!string.IsNullOrEmpty(settings.LogoUrl))
                    {
                        row.ConstantItem(60).Image(settings.LogoUrl);
                    }

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(business.BusinessName).FontSize(18).Bold().FontColor(settings.PrimaryColorHex);
                        if (settings.ShowBusinessAddress && !string.IsNullOrEmpty(business.Address))
                            col.Item().Text(business.Address).FontSize(9);
                    });

                    row.ConstantItem(120).AlignRight().Column(col =>
                    {
                        col.Item().Text(sale.InvoiceNumber).Bold();
                        col.Item().Text(sale.Date.ToString("dd MMM yyyy"));
                    });
                });

                page.Content().PaddingVertical(20).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(settings.PrimaryColorHex).Padding(5).Text("Item").FontColor(Colors.White);
                        header.Cell().Background(settings.PrimaryColorHex).Padding(5).Text("Qty").FontColor(Colors.White);
                        header.Cell().Background(settings.PrimaryColorHex).Padding(5).Text("Unit Price").FontColor(Colors.White);
                        header.Cell().Background(settings.PrimaryColorHex).Padding(5).Text("Total").FontColor(Colors.White);
                    });

                    foreach (var item in sale.Items)
                    {
                        table.Cell().Padding(5).Text(item.Product.ProductName);
                        table.Cell().Padding(5).Text(item.Quantity.ToString());
                        table.Cell().Padding(5).Text(item.UnitPrice.ToString("C"));
                        table.Cell().Padding(5).Text((item.Quantity * item.UnitPrice).ToString("C"));
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().AlignRight().Text($"Total: {sale.TotalAmount:C}").Bold().FontSize(14);
                    if (!string.IsNullOrEmpty(settings.FooterNote))
                        col.Item().PaddingTop(10).Text(settings.FooterNote).FontSize(9).Italic();
                });
            });
        });

        return document.GeneratePdf();
    }
}