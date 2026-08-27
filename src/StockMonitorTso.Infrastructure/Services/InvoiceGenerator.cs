using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Services;

public sealed class InvoiceGenerator
{
    public byte[] Generate(TransportOrder order)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Draft Invoice Pengiriman — Transport Shipping Order").FontSize(16).SemiBold();
                    col.Item().Text($"Order No: {order.OrderNo}    |    Mitra TSO: {order.MitraNamaSnapshot} ({order.MitraId})").FontSize(9);
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(140);
                        columns.RelativeColumn();
                    });

                    table.Cell().Element(CellStyle).Text("Mitra TSO");
                    table.Cell().Element(CellStyle).Text($"{order.MitraNamaSnapshot} ({order.MitraId})");

                    table.Cell().Element(CellStyle).Text("Gudang Wilayah Tujuan");
                    table.Cell().Element(CellStyle).Text(order.RuteTujuan);

                    table.Cell().Element(CellStyle).Text("Jenis Material");
                    table.Cell().Element(CellStyle).Text(order.Produk.DisplayName());

                    table.Cell().Element(CellStyle).Text("Kuantitas + Satuan");
                    table.Cell().Element(CellStyle).Text($"{order.Kuantitas.ToString("0.##")} {order.Satuan}");

                    table.Cell().Element(CellStyle).Text("Tanggal Keberangkatan");
                    table.Cell().Element(CellStyle).Text(order.TanggalKeberangkatan.ToString("dd MMM yyyy"));

                    table.Cell().Element(CellStyle).Text("ETA Estimasi");
                    table.Cell().Element(CellStyle).Text(order.Eta.ToString("dd MMM yyyy"));

                    table.Cell().Element(CellStyle).Text("Nomor Order");
                    table.Cell().Element(CellStyle).Text(order.OrderNo);

                    table.Cell().Element(CellStyle).Text("Timestamp Generate");
                    table.Cell().Element(CellStyle).Text(order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");

                    static IContainer CellStyle(IContainer container) => container
                        .Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Draft Invoice — idempotent").FontSize(8).Italic();
                });
            });
        });

        document.WithMetadata(new DocumentMetadata
        {
            CreationDate = order.CreatedAt,
            ModifiedDate = order.CreatedAt,
            Title = $"Draft Invoice {order.OrderNo}",
            Author = "Stock Monitor dan TSO",
        });

        return document.GeneratePdf();
    }
}
