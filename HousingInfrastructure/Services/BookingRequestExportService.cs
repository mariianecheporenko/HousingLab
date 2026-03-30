using ClosedXML.Excel;
using HousingDomain.Models;
using Microsoft.EntityFrameworkCore;

namespace HousingInfrastructure.Services;

public class BookingRequestExportService : IExportService<BookingRequest>
{
    private static readonly IReadOnlyList<string> HeaderNames =
    [
        "Адреса житла",
        "Дата з",
        "Дата до",
        "Ім'я орендаря",
        "Контакти орендаря",
        "Ціна за весь період"
    ];

    private readonly HousingContext _context;
    private readonly int? _userId;

    public BookingRequestExportService(HousingContext context, int? userId = null)
    {
        _context = context;
        _userId = userId;
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Output stream is not writable.", nameof(stream));
        }

        var bookingsQuery = _context.BookingRequests
            .Include(b => b.Housing)
            .Include(b => b.User)
            .AsNoTracking()
            .OrderBy(b => b.Status)
            .ThenBy(b => b.DateFrom)
            .AsQueryable();

        if (_userId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b => b.UserId == _userId.Value);
        }

        var bookings = await bookingsQuery.ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();

        foreach (var group in bookings.GroupBy(b => string.IsNullOrWhiteSpace(b.Status) ? "Без статусу" : b.Status!))
        {
            var worksheet = workbook.Worksheets.Add(GetSafeWorksheetName(group.Key));
            WriteHeader(worksheet);

            var rowIndex = 2;
            foreach (var booking in group)
            {
                WriteBookingRow(worksheet, booking, rowIndex++);
            }

            worksheet.Columns().AdjustToContents();
        }

        if (workbook.Worksheets.Count == 0)
        {
            var emptyWorksheet = workbook.Worksheets.Add("Без бронювань");
            WriteHeader(emptyWorksheet);
            emptyWorksheet.Columns().AdjustToContents();
        }

        workbook.SaveAs(stream);
    }

    private static void WriteHeader(IXLWorksheet worksheet)
    {
        for (var columnIndex = 0; columnIndex < HeaderNames.Count; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).Value = HeaderNames[columnIndex];
        }

        worksheet.Row(1).Style.Font.Bold = true;
    }

    private static void WriteBookingRow(IXLWorksheet worksheet, BookingRequest booking, int rowIndex)
    {
        var totalPrice = CalculateTotalPrice(booking);
        worksheet.Cell(rowIndex, 1).Value = booking.Housing?.Address ?? "-";
        worksheet.Cell(rowIndex, 2).Value = booking.DateFrom.ToString("yyyy-MM-dd");
        worksheet.Cell(rowIndex, 3).Value = booking.DateTo.ToString("yyyy-MM-dd");
        worksheet.Cell(rowIndex, 4).Value = booking.User?.Name ?? booking.User?.UserName ?? "-";
        worksheet.Cell(rowIndex, 5).Value = GetTenantContacts(booking.User);
        worksheet.Cell(rowIndex, 6).Value = totalPrice;
        worksheet.Cell(rowIndex, 6).Style.NumberFormat.Format = "#,##0.00";
    }

    private static decimal CalculateTotalPrice(BookingRequest booking)
    {
        var dailyPrice = booking.Housing?.Price ?? 0m;
        var days = booking.DateTo.DayNumber - booking.DateFrom.DayNumber + 1;
        return days > 0 ? dailyPrice * days : 0m;
    }

    private static string GetTenantContacts(User? user)
    {
        if (user is null)
        {
            return "-";
        }

        return string.Join(", ",
            new[] { user.Email, user.PhoneNumber }
                .Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    private static string GetSafeWorksheetName(string value)
    {
        var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Без статусу";
        }

        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }
}
