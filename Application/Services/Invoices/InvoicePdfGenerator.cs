using Application.DTOs.Invoices.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Services.Invoices
{
    public static class InvoicePdfGenerator
    {
        static InvoicePdfGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] Generate(InvoiceDto invoice)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                    page.Header().Element(c => ComposeHeader(c, invoice));
                    page.Content().Element(c => ComposeContent(c, invoice));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("SmartInventory · Factura generada automaticamente · Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, InvoiceDto invoice)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("SMARTINVENTORY").Bold().FontSize(18).FontColor(Colors.Teal.Darken2);
                        left.Item().Text("Factura de venta").FontSize(11).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(180).AlignRight().Column(right =>
                    {
                        right.Item().Text(invoice.InvoiceNumber).Bold().FontSize(14).FontColor(Colors.Teal.Darken2);
                        right.Item().Text($"Fecha: {invoice.IssueDate:dd/MM/yyyy}").FontSize(9);
                        right.Item().Text($"Venta #{invoice.SaleId}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                col.Item().PaddingVertical(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private static void ComposeContent(IContainer container, InvoiceDto invoice)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(12).Column(info =>
                {
                    info.Item().Text("Cliente").SemiBold().FontSize(9).FontColor(Colors.Grey.Darken1);
                    info.Item().Text(string.IsNullOrWhiteSpace(invoice.CustomerName) ? "Cliente" : invoice.CustomerName)
                        .Bold()
                        .FontSize(12);
                    if (!string.IsNullOrWhiteSpace(invoice.PaymentMethod))
                        info.Item().Text($"Metodo de pago: {invoice.PaymentMethod}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(invoice.DeliveryAddress))
                        info.Item().Text($"Entrega: {invoice.DeliveryAddress}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(invoice.ContactPhone))
                        info.Item().Text($"Telefono: {invoice.ContactPhone}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(invoice.ContactDocument))
                        info.Item().Text($"Documento: {invoice.ContactDocument}").FontSize(9);
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Producto");
                        header.Cell().Element(HeaderCell).AlignCenter().Text("Cant.");
                        header.Cell().Element(HeaderCell).AlignRight().Text("P. unitario");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                    });

                    foreach (var item in invoice.Items)
                    {
                        table.Cell().Element(BodyCell).Text(item.ProductName);
                        table.Cell().Element(BodyCell).AlignCenter().Text(item.Quantity.ToString());
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(item.UnitPrice));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(item.Subtotal));
                    }
                });

                col.Item().PaddingTop(16).AlignRight().Column(totals =>
                {
                    totals.Item().Row(row =>
                    {
                        row.ConstantItem(100).Text("TOTAL").Bold().FontSize(12);
                        row.ConstantItem(110).AlignRight().Text(FormatMoney(invoice.Total)).Bold().FontSize(14)
                            .FontColor(Colors.Teal.Darken2);
                    });
                });

                col.Item().PaddingTop(24).Text("Documento generado por SmartInventory. Simulacion de facturacion.")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
            });
        }

        private static IContainer HeaderCell(IContainer container) =>
            container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(Colors.Grey.Lighten4)
                .Padding(6)
                .DefaultTextStyle(x => x.SemiBold().FontSize(9));

        private static IContainer BodyCell(IContainer container) =>
            container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten3)
                .PaddingVertical(6)
                .PaddingHorizontal(4);

        private static string FormatMoney(decimal value) =>
            value.ToString("C0", new System.Globalization.CultureInfo("es-CO"));
    }
}
