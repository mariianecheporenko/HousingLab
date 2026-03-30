using ClosedXML.Excel;
using HousingDomain.Models;
using Microsoft.EntityFrameworkCore;

namespace HousingInfrastructure.Services;

public class BookingRequestImportService : IImportService<BookingRequest>
{
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

        foreach (var worksheet in workbook.Worksheets)
        {
            var status = ParseStatus(worksheet.Name);
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                await AddBookingRequestAsync(row, status, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AddBookingRequestAsync(IXLRow row, string status, CancellationToken cancellationToken)
    {
        var address = row.Cell(1).GetString().Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        if (!DateOnly.TryParse(row.Cell(2).GetString(), out var dateFrom) ||
            !DateOnly.TryParse(row.Cell(3).GetString(), out var dateTo))
        {
            throw new FormatException($"Невірний формат дат у рядку {row.RowNumber()}.");
        }

        var tenantName = row.Cell(4).GetString().Trim();
        var contacts = row.Cell(5).GetString().Trim();

        var housing = await GetHousingAsync(address, cancellationToken);
        var user = await GetOrCreateUserAsync(tenantName, contacts, cancellationToken);

        var exists = await _context.BookingRequests.AnyAsync(b =>
            b.HousingId == housing.Id &&
            b.UserId == user.Id &&
            b.DateFrom == dateFrom &&
            b.DateTo == dateTo,
            cancellationToken);

        if (exists)
        {
            return;
        }

        var booking = new BookingRequest
        {
            HousingId = housing.Id,
            UserId = user.Id,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Status = status
        };

        _context.BookingRequests.Add(booking);
    }

    private async Task<Housing> GetHousingAsync(string address, CancellationToken cancellationToken)
    {
        var housing = await _context.Housings.FirstOrDefaultAsync(h => h.Address == address, cancellationToken);
        if (housing is not null)
        {
            return housing;
        }

        housing = new Housing
        {
            Address = address,
            IsAvailable = true,
            Description = "Created from Excel import"
        };

        _context.Housings.Add(housing);
        await _context.SaveChangesAsync(cancellationToken);
        return housing;
    }

    private async Task<User> GetOrCreateUserAsync(string tenantName, string contacts, CancellationToken cancellationToken)
    {
        var email = contacts.Split(',', StringSplitOptions.TrimEntries)
            .FirstOrDefault(c => c.Contains('@'));

        User? user = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            UserName = !string.IsNullOrWhiteSpace(email) ? email : $"imported_{Guid.NewGuid():N}@example.com",
            Email = !string.IsNullOrWhiteSpace(email) ? email : $"imported_{Guid.NewGuid():N}@example.com",
            NormalizedEmail = !string.IsNullOrWhiteSpace(email) ? email.ToUpperInvariant() : null,
            NormalizedUserName = !string.IsNullOrWhiteSpace(email) ? email.ToUpperInvariant() : null,
            PhoneNumber = contacts,
            Name = string.IsNullOrWhiteSpace(tenantName) ? "Imported Tenant" : tenantName,
            BirthDate = new DateOnly(2000, 1, 1),
            Gender = "Unknown",
            Role = "USER",
            WantsToBeOwner = false,
            IsOwnerApproved = false,
            EmailConfirmed = !string.IsNullOrWhiteSpace(email)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
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
