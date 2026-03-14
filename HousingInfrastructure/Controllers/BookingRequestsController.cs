 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HousingDomain.Models;
using HousingInfrastructure;

namespace HousingInfrastructure.Controllers
{
    public class BookingRequestsController : Controller
    {
        private readonly HousingContext _context;

        public BookingRequestsController(HousingContext context)
        {
            _context = context;
        }

        // GET: BookingRequests
        public async Task<IActionResult> Index()
        {
            await SyncStatusesAsync();
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
        public async Task<IActionResult> Create()
        {
            await SyncStatusesAsync();
            ViewData["HousingId"] = new SelectList(_context.Housings.Where(h => h.IsAvailable == true), "Id", "Address"); 
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
           
            return View();
        }

        // POST: BookingRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,HousingId,DateFrom,DateTo,Id")] BookingRequest bookingRequest)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            if (bookingRequest.DateFrom < today)
            {
                ModelState.AddModelError("DateFrom", "Start date cannot be earlier than today.");
            }

            if (bookingRequest.DateTo <= bookingRequest.DateFrom)
            {
                ModelState.AddModelError("DateTo", "End date must be after start date.");
            }

            if (ModelState.IsValid)
            {
                bookingRequest.Status = GetBookingStatus(bookingRequest, today);
                _context.Add(bookingRequest);
                await _context.SaveChangesAsync();

                await SyncStatusesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["HousingId"] = new SelectList(_context.Housings.Where(h => h.IsAvailable == true), "Id", "Address", bookingRequest.HousingId); 
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", bookingRequest.UserId);
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
    }
}
