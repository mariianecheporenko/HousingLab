using HousingDomain.Models;
using HousingInfrastructure;
using HousingInfrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

namespace HousingInfrastructure.Controllers
{
    public class UsersController : Controller
    {
        private readonly HousingContext _context;
        private readonly UserManager<User> _userManager;

        public UsersController(HousingContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Users
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Role != "ADMIN" && currentUser?.Role != "ADMIN")
            {
                return RedirectToAction("Index", "Home"); // Якщо не адмін - викидаємо на головну
            }

            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var profile = await EnsureProfileAsync(user);
            await BuildCompatibilityData(profile, user.Id);

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View(new User
            {
                Role = "Renter",
                IsOwnerApproved = false,
                WantsToBeOwner = false
            });
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,Name,BirthDate,Gender,WantsToBeOwner,Id")] User user)
        {
            if (ModelState.IsValid)
            {
                user.Role = user.WantsToBeOwner ? "OwnerCandidate" : "Renter";
                user.IsOwnerApproved = false;
                _context.Add(user);
                await _context.SaveChangesAsync();
                var profile = CreateDefaultProfile(user.Id);
                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = user.Id });
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Email,Name,BirthDate,Gender,WantsToBeOwner,IsOwnerApproved,Role,Id")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (user.WantsToBeOwner && user.IsOwnerApproved)
                    {
                        user.Role = "Owner";
                    }
                    else if (user.WantsToBeOwner)
                    {
                        user.Role = "OwnerCandidate";
                    }
                    else
                    {
                        user.Role = "Renter";
                        user.IsOwnerApproved = false;
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOwner(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.WantsToBeOwner)
            {
                user.IsOwnerApproved = true;
                user.Role = "Owner";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Profile> EnsureProfileAsync(User user)
        {
            if (user.Profile != null)
            {
                return user.Profile;
            }

            var profile = CreateDefaultProfile(user.Id);
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
            user.Profile = profile;
            return profile;
        }

        private async Task BuildCompatibilityData(Profile me, int currentUserId)
        {
            var others = await _context.Profiles
                .Include(p => p.User)
                .Where(p => p.UserId != currentUserId)
                .ToListAsync();

            var compatibility = others
                .Select(other => new CompatibilityResult
                {
                    UserId = other.UserId,
                    UserName = other.User.Name,
                    UserEmail = other.User.Email,
                    Score = CompatibilityService.Calculate(me, other)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            ViewBag.Compatibility = compatibility;
        }

        private static Profile CreateDefaultProfile(int userId) => new()
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

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
        public class CompatibilityResult
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string UserEmail { get; set; } = string.Empty;
            public int Score { get; set; }
        }
    }
}
