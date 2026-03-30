using HousingDomain.Models;
using HousingInfrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HousingInfrastructure.Services;


namespace HousingInfrastructure.Controllers
{
    public class BookingRequestsController : Controller
    {
        private readonly HousingContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IDataPortServiceFactory<BookingRequest> _bookingRequestDataPortServiceFactory;

        public BookingRequestsController(
                    HousingContext context,
                    UserManager<User> userManager,
                    IDataPortServiceFactory<BookingRequest> bookingRequestDataPortServiceFactory)
        {
            _context = context;
            _userManager = userManager;
            _bookingRequestDataPortServiceFactory = bookingRequestDataPortServiceFactory;
        }

        // GET: BookingRequests
        public async Task<IActionResult> Index()
        {
            await SyncStatusesAsync();
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.IsAdmin = currentUser?.Role == "ADMIN";
            if ((bool)ViewBag.IsAdmin)
            {
                ViewBag.Users = new SelectList(
                    await _context.Users
                        .OrderBy(u => u.Email)
                        .Select(u => new { u.Id, DisplayName = $"{u.Email} ({u.Name})" })
                        .ToListAsync(),
                    "Id",
                    "DisplayName");
            }
            var housingContext = _context.BookingRequests.Include(b => b.Housing).Include(b => b.User);
            return View(await housingContext.ToListAsync());
        }

        // GET: BookingRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            await SyncStatusesAsync();

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Housing)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bookingRequest == null)
            {
                return NotFound();
            }

            return View(bookingRequest);
        }

        // GET: BookingRequests/Create
        [Authorize]
        public async Task<IActionResult> Create(int? housingId)
        {
            if (housingId == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var housing = await _context.Housings.FindAsync(housingId);

            if (housing?.OwnerId == currentUser.Id)
            {
                TempData["Error"] = "Ви не можете забронювати власне житло!";
                return RedirectToAction("Details", "Housings", new { id = housingId });
            }

            // Передаємо ID житла у View
            ViewBag.HousingId = housingId;
            return View();
        }

        // POST: BookingRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("DateFrom,DateTo")] BookingRequest bookingRequest, int housingId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            bookingRequest.UserId = currentUser.Id;
            bookingRequest.HousingId = housingId;

            ModelState.Remove("UserId");
            ModelState.Remove("HousingId");
            ModelState.Remove("User");
            ModelState.Remove("Housing");

            if (ModelState.IsValid)
            {
                _context.Add(bookingRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyBookings));
            }
            ViewBag.HousingId = housingId;
            return View(bookingRequest);
        }

        // GET: BookingRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            await SyncStatusesAsync();

            var bookingRequest = await _context.BookingRequests.FindAsync(id);
            if (bookingRequest == null)
            {
                return NotFound();
            }
            ViewData["HousingId"] = new SelectList(_context.Housings, "Id", "Address", bookingRequest.HousingId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", bookingRequest.UserId);
            return View(bookingRequest);
        }

        // POST: BookingRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,HousingId,DateFrom,DateTo,Id")] BookingRequest bookingRequest)
        {
            if (id != bookingRequest.Id)
            {
                return NotFound();
            }

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            if (bookingRequest.DateFrom < today)
            {
                ModelState.AddModelError("DateFrom", "Start date cannot be earlier than today.");
            }

            if (bookingRequest.DateTo <= bookingRequest.DateFrom)
            {
                ModelState.AddModelError("DateTo", "End date must be after start date.");
            }

            await ValidateNotBookingOwnHousingAsync(bookingRequest);

            if (ModelState.IsValid)
            {
                try
                {
                    bookingRequest.Status = GetBookingStatus(bookingRequest, today);

                    _context.Update(bookingRequest);
                    await _context.SaveChangesAsync();
                    await SyncStatusesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingRequestExists(bookingRequest.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["HousingId"] = new SelectList(_context.Housings, "Id", "Address", bookingRequest.HousingId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", bookingRequest.UserId);
            return View(bookingRequest);
        }

        // GET: BookingRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            await SyncStatusesAsync();

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Housing)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bookingRequest == null)
            {
                return NotFound();
            }

            return View(bookingRequest);
        }

        // POST: BookingRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bookingRequest = await _context.BookingRequests.FindAsync(id);
            if (bookingRequest != null)
            {
                _context.BookingRequests.Remove(bookingRequest);
            }

            await _context.SaveChangesAsync();
            await SyncStatusesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateNotBookingOwnHousingAsync(BookingRequest bookingRequest)
        {
            var housingOwnerId = await _context.Housings
                .Where(h => h.Id == bookingRequest.HousingId)
                .Select(h => h.OwnerId)
                .FirstOrDefaultAsync();

            if (housingOwnerId == bookingRequest.UserId)
            {
                ModelState.AddModelError("UserId", "Власник не може бронювати власне житло.");
            }
        }

        private async Task SyncStatusesAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var bookings = await _context.BookingRequests.ToListAsync();
            var statusChanged = false;

            foreach (var booking in bookings)
            {
                var calculatedStatus = GetBookingStatus(booking, today);
                if (booking.Status != calculatedStatus)
                {
                    booking.Status = calculatedStatus;
                    statusChanged = true;
                }
            }

            var occupiedHousingIds = bookings
                .Where(b => b.DateFrom <= today && b.DateTo >= today)
                .Select(b => b.HousingId)
                .Distinct()
                .ToHashSet();

            var housings = await _context.Housings.ToListAsync();
            foreach (var housing in housings)
            {
                var shouldBeAvailable = !occupiedHousingIds.Contains(housing.Id);
                if (housing.IsAvailable != shouldBeAvailable)
                {
                    housing.IsAvailable = shouldBeAvailable;
                    statusChanged = true;
                }
            }

            if (statusChanged)
            {
                await _context.SaveChangesAsync();
            }
        }

        private static string GetBookingStatus(BookingRequest booking, DateOnly today)
        {
            if (booking.DateTo < today)
            {
                return "Expired";
            }

            if (booking.DateFrom > today)
            {
                return "Scheduled";
            }

            return "Active";
        }


        private bool BookingRequestExists(int id)
        {
            return _context.BookingRequests.Any(e => e.Id == id);
        }

        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            var myBookings = await _context.BookingRequests
                .Include(b => b.Housing)
                .Where(b => b.UserId == user.Id)
                .ToListAsync();

            return View(myBookings);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Import()
        {
            var adminCheck = await EnsureAdminAccessAsync();
            if (adminCheck is not null)
            {
                return adminCheck;
            }

            return View("Import");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Import(IFormFile fileExcel, CancellationToken cancellationToken = default)
        {
            var adminCheck = await EnsureAdminAccessAsync();
            if (adminCheck is not null)
            {
                return adminCheck;
            }

            if (fileExcel is null || fileExcel.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Будь ласка, оберіть Excel-файл для імпорту.");
                return View();
            }

            var contentType = NormalizeExcelContentType(fileExcel.ContentType);
            var importService = _bookingRequestDataPortServiceFactory.GetImportService(contentType);
            await using var stream = fileExcel.OpenReadStream();
            await importService.ImportFromStreamAsync(stream, cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Export(
            [FromQuery] string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            var adminCheck = await EnsureAdminAccessAsync();
            if (adminCheck is not null)
            {
                return adminCheck;
            }

            contentType = NormalizeExcelContentType(contentType);
            var exportService = userId.HasValue
                ? new BookingRequestExportService(_context, userId)
                : _bookingRequestDataPortServiceFactory.GetExportService(contentType);
            var memoryStream = new MemoryStream();

            await exportService.WriteToAsync(memoryStream, cancellationToken);
            await memoryStream.FlushAsync(cancellationToken);
            memoryStream.Position = 0;

            return new FileStreamResult(memoryStream, contentType)
            {
                FileDownloadName = userId.HasValue
                    ? $"booking_report_user_{userId.Value}_{DateTime.UtcNow:yyyyMMdd}.xlsx"
                    : $"booking_report_{DateTime.UtcNow:yyyyMMdd}.xlsx"
            };
        }

        private async Task<IActionResult?> EnsureAdminAccessAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Role != "ADMIN")
            {
                return Forbid();
            }

            return null;
        }

        private static string NormalizeExcelContentType(string? contentType)
        {
            const string excelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return string.IsNullOrWhiteSpace(contentType) || contentType == "application/octet-stream"
                ? excelContentType
                : contentType;
        }
    }
}