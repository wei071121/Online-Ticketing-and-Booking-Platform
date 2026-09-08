using System.Globalization;
using System.Text;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Services;

public interface IReceiptPdfService
{
    byte[] Create(Payment payment);
}

public class ReceiptPdfService : IReceiptPdfService
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double Left = 42;
    private const double Right = 553;
    private const double TableBottom = 315;

    public byte[] Create(Payment payment)
    {
        var rows = BuildRows(payment);
        var pages = new List<StringBuilder>();
        var page = BeginPage(payment, PageSection.First);
        var y = 600d;

        foreach (var row in rows)
        {
            var height = MeasureRow(row);
            if (y - height < TableBottom)
            {
                pages.Add(page);
                page = BeginPage(payment, PageSection.ItemsContinued);
                y = 650;
            }

            DrawRow(page, row, y, height);
            y -= height;
        }

        // Keep totals together. A full item page gets a clean summary page.
        if (y < TableBottom + 8)
        {
            pages.Add(page);
            page = BeginPage(payment, PageSection.Summary);
        }

        DrawSummary(page, payment);
        pages.Add(page);

        for (var index = 0; index < pages.Count; index++)
        {
            DrawFooter(pages[index], payment, index + 1, pages.Count);
        }

        return BuildPdf(pages.Select(item => item.ToString()).ToList());
    }

    private static List<ReceiptRow> BuildRows(Payment payment)
    {
        var rows = new List<ReceiptRow>();
        foreach (var order in payment.DiningSession.Orders
                     .Where(item => item.Status != "Cancelled")
                     .OrderBy(item => item.OrderedAt))
        {
            rows.Add(new ReceiptRow(
                true,
                $"ORDER {order.OrderNumber} - {order.OrderedAt:dd MMM yyyy HH:mm}",
                0,
                0,
                0,
                []));

            foreach (var item in order.OrderItems)
            {
                var addOnTotal = item.OrderItemAddOns.Sum(addOn => addOn.UnitPrice);
                var unitTotal = item.UnitPrice + addOnTotal;
                var details = item.OrderItemAddOns
                    .Select(addOn => $"Add-on: {addOn.AddOnName} (+RM {addOn.UnitPrice:0.00})")
                    .ToList();

                if (!string.IsNullOrWhiteSpace(item.SpecialRequest))
                {
                    details.Add($"Request: {item.SpecialRequest.Trim()}");
                }

                rows.Add(new ReceiptRow(
                    false,
                    item.MenuItemName,
                    item.Quantity,
                    unitTotal,
                    decimal.Round(item.Quantity * unitTotal, 2),
                    details));
            }
        }

        return rows;
    }

    private static StringBuilder BeginPage(Payment payment, PageSection section)
    {
        var content = new StringBuilder();
        FillRect(content, 0, 750, PageWidth, 92, "0.09 0.30 0.24");
        DrawText(content, "QIGLO RESTAURANT", Left, 800, "F2", 21, "1 1 1");
        DrawText(
            content,
            "TABLE RESERVATION AND IN-HOUSE DINING",
            Left,
            779,
            "F1",
            9,
            "0.86 0.93 0.90");
        DrawRightText(
            content,
            payment.ReceiptNumber,
            Right,
            792,
            "F2",
            10,
            "1 1 1");

        if (section == PageSection.First)
        {
            DrawText(content, "OFFICIAL E-RECEIPT", Left, 720, "F2", 15, "0.09 0.30 0.24");
            DrawMeta(content, "Receipt", payment.ReceiptNumber, Left, 690);
            DrawMeta(
                content,
                "Paid",
                payment.PaidAt?.ToString("dd MMM yyyy, HH:mm") ?? "Pending",
                300,
                690);
            DrawMeta(
                content,
                "Customer",
                payment.DiningSession.Customer?.FullName ?? "Walk-in customer",
                Left,
                656);
            DrawMeta(
                content,
                "Table",
                payment.DiningSession.DiningTable.TableNumber,
                300,
                656);
            DrawTableHeader(content, 632);
        }
        else if (section == PageSection.ItemsContinued)
        {
            DrawText(content, "ORDER ITEMS - CONTINUED", Left, 712, "F2", 14, "0.09 0.30 0.24");
            DrawTableHeader(content, 682);
        }
        else
        {
            DrawText(content, "PAYMENT SUMMARY", Left, 712, "F2", 14, "0.09 0.30 0.24");
        }

        return content;
    }

    private static void DrawMeta(
        StringBuilder content,
        string label,
        string value,
        double x,
        double y)
    {
        DrawText(content, label.ToUpperInvariant(), x, y + 12, "F2", 7.5, "0.40 0.45 0.49");
        DrawText(content, value, x, y - 2, "F1", 10, "0.12 0.16 0.20");
    }

    private static void DrawTableHeader(StringBuilder content, double y)
    {
        FillRect(content, Left, y - 20, Right - Left, 27, "0.92 0.95 0.93");
        DrawText(content, "DESCRIPTION", Left + 10, y - 11, "F2", 8, "0.09 0.30 0.24");
        DrawText(content, "QTY", 389, y - 11, "F2", 8, "0.09 0.30 0.24");
        DrawText(content, "UNIT", 445, y - 11, "F2", 8, "0.09 0.30 0.24");
        DrawRightText(content, "AMOUNT", Right - 8, y - 11, "F2", 8, "0.09 0.30 0.24");
    }

    private static double MeasureRow(ReceiptRow row)
    {
        if (row.IsOrderHeader)
        {
            return 27;
        }

        var descriptionLines = Wrap(row.Description, 43).Count;
        var detailLines = row.Details.Sum(detail => Wrap(detail, 55).Count);
        return 27 + (descriptionLines - 1) * 11 + detailLines * 10;
    }

    private static void DrawRow(
        StringBuilder content,
        ReceiptRow row,
        double y,
        double height)
    {
        if (row.IsOrderHeader)
        {
            FillRect(content, Left, y - height + 4, Right - Left, height - 4, "0.97 0.95 0.89");
            DrawText(content, row.Description, Left + 10, y - 17, "F2", 8.5, "0.38 0.29 0.08");
            return;
        }

        var baseline = y - 15;
        var descriptionLines = Wrap(row.Description, 43);
        for (var index = 0; index < descriptionLines.Count; index++)
        {
            DrawText(
                content,
                descriptionLines[index],
                Left + 10,
                baseline - index * 11,
                index == 0 ? "F2" : "F1",
                9,
                "0.12 0.16 0.20");
        }

        DrawText(content, row.Quantity.ToString(CultureInfo.InvariantCulture), 392, baseline, "F1", 9, "0.12 0.16 0.20");
        DrawRightText(content, $"RM {row.UnitPrice:0.00}", 494, baseline, "F1", 9, "0.12 0.16 0.20");
        DrawRightText(content, $"RM {row.Amount:0.00}", Right - 8, baseline, "F2", 9, "0.12 0.16 0.20");

        var detailY = baseline - descriptionLines.Count * 11;
        foreach (var detail in row.Details)
        {
            foreach (var detailLine in Wrap(detail, 55))
            {
                DrawText(content, detailLine, Left + 20, detailY, "F1", 7.5, "0.40 0.45 0.49");
                detailY -= 10;
            }
        }

        DrawLine(content, Left, y - height + 3, Right, y - height + 3, 0.5, "0.87 0.89 0.90");
    }

    private static void DrawSummary(StringBuilder content, Payment payment)
    {
        const double top = 278;

        DrawText(content, "Thank you for dining with us.", Left, top, "F2", 13, "0.09 0.30 0.24");
        DrawText(content, "Your payment has been completed successfully.", Left, top - 20, "F1", 9, "0.40 0.45 0.49");
        DrawText(content, $"Payment method: {payment.PaymentMethod}", Left, top - 55, "F1", 9, "0.12 0.16 0.20");
        DrawText(content, $"Amount received: RM {payment.AmountReceived:0.00}", Left, top - 72, "F1", 9, "0.12 0.16 0.20");
        DrawText(content, $"Change: RM {payment.ChangeAmount:0.00}", Left, top - 89, "F1", 9, "0.12 0.16 0.20");

        const double summaryX = 315;
        DrawText(content, "PAYMENT SUMMARY", summaryX, top, "F2", 10, "0.09 0.30 0.24");
        DrawSummaryLine(content, "Subtotal", payment.Subtotal, summaryX, top - 25);
        DrawSummaryLine(content, "Tax (6%)", payment.TaxAmount, summaryX, top - 43);
        DrawSummaryLine(content, "Service charge (10%)", payment.ServiceCharge, summaryX, top - 61);

        FillRect(content, summaryX, top - 112, Right - summaryX, 34, "0.09 0.30 0.24");
        DrawText(content, "TOTAL", summaryX + 10, top - 100, "F2", 11, "1 1 1");
        DrawRightText(content, $"RM {payment.TotalAmount:0.00}", Right - 10, top - 100, "F2", 12, "1 1 1");
    }

    private static void DrawSummaryLine(
        StringBuilder content,
        string label,
        decimal amount,
        double x,
        double y)
    {
        DrawText(content, label, x, y, "F1", 9, "0.40 0.45 0.49");
        DrawRightText(content, $"RM {amount:0.00}", Right, y, "F1", 9, "0.12 0.16 0.20");
    }

    private static void DrawFooter(
        StringBuilder content,
        Payment payment,
        int pageNumber,
        int pageCount)
    {
        DrawLine(content, Left, 58, Right, 58, 0.6, "0.83 0.85 0.86");
        DrawText(content, "QIGLO Restaurant e-receipt", Left, 41, "F1", 7.5, "0.40 0.45 0.49");
        DrawRightText(
            content,
            $"{payment.ReceiptNumber}  |  Page {pageNumber} of {pageCount}",
            Right,
            41,
            "F1",
            7.5,
            "0.40 0.45 0.49");
    }

    private static byte[] BuildPdf(IReadOnlyList<string> pageContents)
    {
        var pageIds = Enumerable.Range(0, pageContents.Count)
            .Select(index => 5 + index * 2)
            .ToList();
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pageContents.Count} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
        };

        for (var index = 0; index < pageContents.Count; index++)
        {
            var contentBytes = Encoding.ASCII.GetBytes(pageContents[index]);
            var contentId = pageIds[index] + 1;
            objects.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] " +
                $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>");
            objects.Add(
                $"<< /Length {contentBytes.Length.ToString(CultureInfo.InvariantCulture)} >>\n" +
                $"stream\n{pageContents[index]}endstream");
        }

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n%QIGLO\n");
        var offsets = new List<long> { 0 };

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            WriteAscii(output, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefOffset = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(output, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(output, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(
            output,
            $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return output.ToArray();
    }

    private static void DrawText(
        StringBuilder content,
        string value,
        double x,
        double y,
        string font,
        double size,
        string color)
    {
        content.AppendLine("BT");
        content.Append('/').Append(font).Append(' ').Append(Number(size)).AppendLine(" Tf");
        content.Append(color).AppendLine(" rg");
        content.Append("1 0 0 1 ").Append(Number(x)).Append(' ').Append(Number(y)).AppendLine(" Tm");
        content.Append('(').Append(EscapePdfText(value)).AppendLine(") Tj");
        content.AppendLine("ET");
    }

    private static void DrawRightText(
        StringBuilder content,
        string value,
        double right,
        double y,
        string font,
        double size,
        string color)
    {
        var estimatedWidth = SanitizePdfText(value).Length * size * 0.50;
        DrawText(content, value, right - estimatedWidth, y, font, size, color);
    }

    private static void FillRect(
        StringBuilder content,
        double x,
        double y,
        double width,
        double height,
        string color)
    {
        content.Append(color).AppendLine(" rg");
        content.Append(Number(x)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(width)).Append(' ').Append(Number(height)).AppendLine(" re f");
    }

    private static void DrawLine(
        StringBuilder content,
        double x1,
        double y1,
        double x2,
        double y2,
        double width,
        string color)
    {
        content.Append(color).AppendLine(" RG");
        content.Append(Number(width)).AppendLine(" w");
        content.Append(Number(x1)).Append(' ').Append(Number(y1)).Append(" m ")
            .Append(Number(x2)).Append(' ').Append(Number(y2)).AppendLine(" l S");
    }

    private static List<string> Wrap(string value, int maxCharacters)
    {
        var words = SanitizePdfText(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = new StringBuilder();

        foreach (var word in words)
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > maxCharacters)
            {
                lines.Add(current.ToString());
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            lines.Add(current.ToString());
        }

        return lines.Count == 0 ? [string.Empty] : lines;
    }

    private static string EscapePdfText(string value) => SanitizePdfText(value)
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal)
        .Replace(")", "\\)", StringComparison.Ordinal);

    private static string SanitizePdfText(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            result.Append(character switch
            {
                >= ' ' and <= '~' => character,
                '×' => 'x',
                '–' or '—' => '-',
                '’' or '‘' => '\'',
                '“' or '”' => '"',
                _ => '?'
            });
        }

        return result.ToString();
    }

    private static string Number(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private enum PageSection
    {
        First,
        ItemsContinued,
        Summary
    }

    private sealed record ReceiptRow(
        bool IsOrderHeader,
        string Description,
        int Quantity,
        decimal UnitPrice,
        decimal Amount,
        IReadOnlyList<string> Details);
}
