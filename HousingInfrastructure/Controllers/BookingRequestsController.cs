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
        public IActionResult Create()
        {
            ViewData["HousingId"] = new SelectList(_context.Housings, "Id", "Address");
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
                bookingRequest.Status = "Pending";

                _context.Add(bookingRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["HousingId"] = new SelectList(_context.Housings, "Id", "Address", bookingRequest.HousingId);
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
                    _context.Update(bookingRequest);
                    await _context.SaveChangesAsync();
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
            return RedirectToAction(nameof(Index));
        }

        private bool BookingRequestExists(int id)
        {
            return _context.BookingRequests.Any(e => e.Id == id);
        }
    }
}
