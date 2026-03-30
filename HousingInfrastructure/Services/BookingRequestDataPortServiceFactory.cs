using HousingDomain.Models;

namespace HousingInfrastructure.Services;

public class BookingRequestDataPortServiceFactory : IDataPortServiceFactory<BookingRequest>
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly HousingContext _context;

    public BookingRequestDataPortServiceFactory(HousingContext context)
    {
        _context = context;
    }

    public IImportService<BookingRequest> GetImportService(string contentType)
    {
        if (contentType == ExcelContentType)
        {
            return new BookingRequestImportService(_context);
        }

        throw new NotSupportedException($"No import service implemented for content type '{contentType}'.");
    }

    public IExportService<BookingRequest> GetExportService(string contentType)
    {
        if (contentType == ExcelContentType)
        {
            return new BookingRequestExportService(_context);
        }

        throw new NotSupportedException($"No export service implemented for content type '{contentType}'.");
    }
}
