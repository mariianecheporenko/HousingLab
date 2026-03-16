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
    public class ProfilesController : Controller
    {
        private readonly HousingContext _context;

        public ProfilesController(HousingContext context)
        {
            _context = context;
        }

        // GET: Profiles
        public async Task<IActionResult> Index()
        {
            var housingContext = _context.Profiles.Include(p => p.User);
            return View(await housingContext.ToListAsync());
        }

        // GET: Profiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        // GET: Profiles/Create
        public async Task<IActionResult> Create(int? userId)
        {
            if (userId.HasValue)
            {
                var existing = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId.Value);
                if (existing != null)
                {
                    return RedirectToAction(nameof(Edit), new { id = existing.Id });
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return NotFound();
                }

                var profile = CreateDefaultProfile(user.Id);
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", user.Id);
                return View(profile);
            }


            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View(CreateDefaultProfile());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,NoiseLevel,SleepMode,Pets,Guests,CleanLevel,Smoking,PreferredGender,Id")] Profile profile)
        {
            if (await _context.Profiles.AnyAsync(p => p.UserId == profile.UserId && p.Id != profile.Id))
            {
                ModelState.AddModelError("UserId", "Для цього користувача вже існує профіль.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(profile);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Users", new { id = profile.UserId });
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", profile.UserId);
            return View(profile);
        }


        // GET: Profiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", profile.UserId);
            return View(profile);
        }

        // POST: Profiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,NoiseLevel,SleepMode,Pets,Guests,CleanLevel,Smoking,PreferredGender,Id")] Profile profile)
        {
            if (id != profile.Id)
            {
                return NotFound();
            }
            if (await _context.Profiles.AnyAsync(p => p.UserId == profile.UserId && p.Id != profile.Id))
            {
                ModelState.AddModelError("UserId", "Для цього користувача вже існує профіль.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProfileExists(profile.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Users", new { id = profile.UserId });
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", profile.UserId);
            return View(profile);
        }

        // GET: Profiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        // POST: Profiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.Profiles.FindAsync(id);
            if (profile != null)
            {
                _context.Profiles.Remove(profile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private static Profile CreateDefaultProfile(int userId = 0) => new()
        {
            UserId = userId,
            NoiseLevel = "Medium",
            SleepMode = "Flexible",
            Pets = false,
            Guests = "Sometimes",
            CleanLevel = "Medium",
            Smoking = "No",
            PreferredGender = "Any"
        };

        private bool ProfileExists(int id)
        {
            return _context.Profiles.Any(e => e.Id == id);
        }
    }
}
