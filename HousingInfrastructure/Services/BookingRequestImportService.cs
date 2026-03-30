using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using HousingDomain.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;

namespace HousingInfrastructure.Services;

public class BookingRequestImportService : IImportService<BookingRequest>
{
    private static readonly string[] SupportedDateFormats =
    [
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "dd/MM/yyyy",
        "MM/dd/yyyy"
    ];
    private readonly HousingContext _context;

    public BookingRequestImportService(HousingContext context)
    {
        _context = context;
    }

    public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanRead)
        {
            throw new ArgumentException("Input stream is not readable.", nameof(stream));
        }

        using var workbook = new XLWorkbook(stream);
        var validationErrors = new List<string>();
        var bookingsToImport = new List<BookingRequest>();

        foreach (var worksheet in workbook.Worksheets)
        {
            var status = ParseStatus(worksheet.Name);
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var booking = await BuildBookingRequestAsync(
                                    row,
                                    status,
                                    bookingsToImport,
                                    validationErrors,
                                    cancellationToken);

                if (booking is not null)
                {
                    bookingsToImport.Add(booking);
                }
            }
        }

        if (validationErrors.Count > 0)
        {
            throw new ImportValidationException(validationErrors);
        }

        if (bookingsToImport.Count == 0)
        {
            return;
        }

        _context.BookingRequests.AddRange(bookingsToImport);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<BookingRequest?> BuildBookingRequestAsync(
           IXLRow row,
           string status,
           IReadOnlyCollection<BookingRequest> pendingBookings,
           ICollection<string> validationErrors,
           CancellationToken cancellationToken)
    {
        var address = row.Cell(1).GetString().Trim();
        var rowNumber = row.RowNumber();
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        if (!DateOnly.TryParse(row.Cell(2).GetString(), out var dateFrom) ||
            !DateOnly.TryParse(row.Cell(3).GetString(), out var dateTo))
        {
            validationErrors.Add($"Рядок {rowNumber}: невірний формат дат.");
            return null;
        }

        if (dateTo < dateFrom)
        {
            validationErrors.Add($"Рядок {rowNumber}: дата завершення має бути пізніше дати початку.");
            return null;
        }

        var tenantName = row.Cell(4).GetString().Trim();
        var contacts = row.Cell(5).GetString().Trim();
        var email = ExtractEmail(contacts);

        if (string.IsNullOrWhiteSpace(email))
        {
            validationErrors.Add($"Рядок {rowNumber}: не знайдено валідну електронну пошту.");
            return null;
        }

        var housing = await _context.Housings.FirstOrDefaultAsync(h => h.Address == address, cancellationToken);
        if (housing is null)
        {
            validationErrors.Add($"Рядок {rowNumber}: житло за адресою \"{address}\" не знайдено.");
            return null;
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.Email != null &&
                 u.Name != null &&
                 u.Email.ToLower() == email.ToLower() &&
                 u.Name.ToLower() == tenantName.ToLower(),
            cancellationToken);

        if (user is null)
        {
            validationErrors.Add($"Рядок {rowNumber}: користувача з іменем \"{tenantName}\" та email \"{email}\" не знайдено.");
            return null;
        }

        if (!string.Equals(user.UserName, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            validationErrors.Add($"Рядок {rowNumber}: для користувача \"{email}\" ім'я користувача має збігатися з поштою.");
            return null;
        }


        if (housing.OwnerId == user.Id)
        {
            validationErrors.Add($"Рядок {rowNumber}: не можна бронювати власне житло.");
            return null;
        }

        if (housing.IsAvailable == false)
        {
            validationErrors.Add($"Рядок {rowNumber}: житло \"{address}\" недоступне.");
            return null;
        }

        var exists = await _context.BookingRequests.AnyAsync(b =>
            b.HousingId == housing.Id &&
            b.UserId == user.Id &&
            b.DateFrom == dateFrom &&
            b.DateTo == dateTo,
            cancellationToken);

        if (exists)
        {
            return null;
        }

        var duplicateInBatch = pendingBookings.Any(b =>
                 b.HousingId == housing.Id &&
                 b.UserId == user.Id &&
                 b.DateFrom == dateFrom &&
                 b.DateTo == dateTo);

        if (duplicateInBatch) {
            return null;
        }


        var occupiedPlacesInDb = await _context.BookingRequests.CountAsync(
                    b => b.HousingId == housing.Id &&
                         b.DateFrom <= dateTo &&
                         b.DateTo >= dateFrom,
                    cancellationToken);

        var occupiedPlacesInBatch = pendingBookings.Count(
            b => b.HousingId == housing.Id &&
                 b.DateFrom <= dateTo &&
                 b.DateTo >= dateFrom);

        var maxPlaces = Math.Max(1, housing.Rooms ?? 1);
        if (occupiedPlacesInDb + occupiedPlacesInBatch >= maxPlaces)
        {
            validationErrors.Add($"Рядок {rowNumber}: для житла \"{address}\" немає вільних місць на обрані дати.");
            return null;
        }

        return new BookingRequest
        {
            HousingId = housing.Id,
            UserId = user.Id,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Status = status
        };

    }


    private static string? ExtractEmail(string contacts)
    {
        return contacts.Split(',', StringSplitOptions.TrimEntries)
            .FirstOrDefault(c => c.Contains('@'));
    }
    private static bool TryReadDate(IXLCell cell, out DateOnly date)
    {
        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime.Date);
            return true;
        }
        if (cell.TryGetValue<double>(out var excelDateValue))
        {
            try
            {
                var excelDate = DateTime.FromOADate(excelDateValue);
                date = DateOnly.FromDateTime(excelDate.Date);
                return true;
            }
            catch (ArgumentException)
            {
                // ignored: continue with string parsing below
            }
        }
        var rawValue = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            date = default;
            return false;
        }
        var cultures = new[] { CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("uk-UA"), CultureInfo.GetCultureInfo("en-US") };
        foreach (var culture in cultures)
        {
            if (DateOnly.TryParseExact(rawValue, SupportedDateFormats, culture, DateTimeStyles.None, out date))
            {
                return true;
            }
            if (DateOnly.TryParse(rawValue, culture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }
        date = default;
        return false;
    }

    private static string ParseStatus(string worksheetName)
    {
        return worksheetName switch
        {
            "Підтверджено" => "Active",
            "Очікує" => "Scheduled",
            "Завершено" => "Expired",
            _ => worksheetName
        };
    }
}
