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
    public class HousingsController : Controller
    {
        private readonly HousingContext _context;

        public HousingsController(HousingContext context)
        {
            _context = context;
        }

        // GET: Housings
        public async Task<IActionResult> Index()
        {
            var housingContext = _context.Housings.Include(h => h.Owner);
            return View(await housingContext.ToListAsync());
        }

        // GET: Housings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housing = await _context.Housings
                .Include(h => h.Owner)
                .Include(h => h.Reviews)
                    .ThenInclude(r => r.User)
                .Include(h => h.BookingRequests)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (housing == null)
            {
                return NotFound();
            }

            return View(housing);
        }

        // GET: Housings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Housings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Address,City,Price,Rooms,Area,IsAvailable,Description,OwnerId,Id")] Housing housing)
        {
            if (ModelState.IsValid)
            {
                _context.Add(housing);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(housing);
        }

        // GET: Housings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housing = await _context.Housings.FindAsync(id);
            if (housing == null)
            {
                return NotFound();
            }
            return View(housing);
        }

        // POST: Housings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Address,City,Price,Rooms,Area,IsAvailable,Description,OwnerId,Id")] Housing housing)
        {
            if (id != housing.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(housing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HousingExists(housing.Id))
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
            return View(housing);
        }

        // GET: Housings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housing = await _context.Housings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (housing == null)
            {
                return NotFound();
            }

            return View(housing);
        }

        // POST: Housings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var housing = await _context.Housings.FindAsync(id);
            if (housing != null)
            {
                _context.Housings.Remove(housing);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HousingExists(int id)
        {
            return _context.Housings.Any(e => e.Id == id);
        }
    }
}
