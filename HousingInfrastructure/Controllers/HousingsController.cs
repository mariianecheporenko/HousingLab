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
using System.Threading.Tasks;

namespace HousingInfrastructure.Controllers
{
    public class HousingsController : Controller
    {
        private readonly HousingContext _context;
        private readonly UserManager<User> _userManager;


        public HousingsController(HousingContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Housings
        public async Task<IActionResult> Index(int? userId)
        {
            await SyncHousingAvailabilityAsync();
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Name)
                .ThenBy(u => u.Email)
                .ToListAsync();
            ViewData["UserId"] = new SelectList(users, "Id", "Email", userId);
            ViewBag.SelectedUserId = userId;

            var housings = await _context.Housings
                .Include(h => h.Owner)
                .Include(h => h.BookingRequests)
                .ToListAsync();

            if (userId.HasValue)
            {
                var selectedUserId = userId.Value;
                var selectedProfile = await _context.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == selectedUserId);

                var neighborProfiles = await _context.Profiles
                    .Include(p => p.User)
                    .ToDictionaryAsync(p => p.UserId);

                housings = housings
                    .Select(h => new
                    {
                        Housing = h,
                        Score = BuildHousingScore(h, selectedUserId, selectedProfile, neighborProfiles)
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Housing.Price ?? decimal.MaxValue)
                    .Select(x => x.Housing)
                    .ToList();
            }
            else
            {
                housings = housings
                    .OrderByDescending(h => h.IsAvailable == true)
                    .ThenBy(h => h.Price ?? decimal.MaxValue)
                    .ToList();
            }

            return View(housings);
        }


        private static double BuildHousingScore(
            Housing housing,
            int selectedUserId,
            Profile? selectedProfile,
            IReadOnlyDictionary<int, Profile> allProfiles)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var score = 0.0;

            if (housing.OwnerId == selectedUserId)
            {
                score -= 1000;
            }

            if (housing.IsAvailable == true)
            {
                score += 80;
            }

            var ownBookings = housing.BookingRequests.Count(b => b.UserId == selectedUserId);
            if (ownBookings > 0)
            {
                score += 30 + (ownBookings * 5);
            }

            var activeOrUpcomingNeighborIds = housing.BookingRequests
                .Where(b => b.UserId != selectedUserId && b.DateTo >= today)
                .Select(b => b.UserId)
                .Distinct()
                .ToList();

            if (selectedProfile != null && activeOrUpcomingNeighborIds.Count > 0)
            {
                var compatibilityValues = activeOrUpcomingNeighborIds
                    .Where(id => allProfiles.ContainsKey(id))
                    .Select(id => CompatibilityService.Calculate(selectedProfile, allProfiles[id]))
                    .ToList();

                if (compatibilityValues.Count > 0)
                {
                    score += compatibilityValues.Average();
                }
            }

            if (housing.Price.HasValue)
            {
                score -= (double)housing.Price.Value / 1000.0;
            }

            return score;
        }

        // GET: Housings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            await SyncHousingAvailabilityAsync();

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
        [Authorize]
        public async Task<IActionResult> Create()

        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = currentUser?.Role == "ADMIN";
            var canCreateAsOwner = currentUser is not null && currentUser.WantsToBeOwner && currentUser.IsOwnerApproved;

            if (currentUser == null || (!isAdmin && !canCreateAsOwner))
            {
                TempData["Error"] = "Тільки підтверджені власники можуть додавати житло.";
                return RedirectToAction(nameof(Index)); 
            }
            SetCitiesSelectList();
            SetOwnersSelectList();

            return View();
        }

        // POST: Housings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Id,Address,City,Description,Price,Rooms,Area,IsAvailable")] Housing housing)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = currentUser?.Role == "ADMIN";
            var canCreateAsOwner = currentUser is not null && currentUser.WantsToBeOwner && currentUser.IsOwnerApproved;

            if (currentUser == null || (!isAdmin && !canCreateAsOwner))
            {
                return Forbid();
            }

            housing.OwnerId = canCreateAsOwner ? currentUser.Id : null;

            ModelState.Remove("OwnerId");
            ModelState.Remove("Owner");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(housing);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Не вдалося зберегти житло. Перевірте, що всі обов'язкові поля заповнені коректно.");
                }
                SetCitiesSelectList(housing.City);
                SetOwnersSelectList(housing.OwnerId);

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
            var currentUser = await _userManager.GetUserAsync(User);
            if (housing.OwnerId != currentUser?.Id && currentUser?.Role != "ADMIN")
            {
                return Forbid();
            }
            if (housing == null)
            {
                return NotFound();
            }
            SetCitiesSelectList(housing.City);
            SetOwnersSelectList(housing.OwnerId);
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

            if (housing.OwnerId.HasValue)
            {
                var ownerAllowed = await _context.Users.AnyAsync(u =>
                    u.Id == housing.OwnerId.Value && u.WantsToBeOwner && u.IsOwnerApproved);
                var currentUser = await _userManager.GetUserAsync(User);
                if (housing.OwnerId != currentUser?.Id && currentUser?.Role != "ADMIN")
                {
                    return Forbid();
                }

                if (!ownerAllowed)
                {
                    ModelState.AddModelError("OwnerId", "Житло може бути прив'язане лише до підтвердженого власника.");
                }
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
            SetCitiesSelectList(housing.City);
            SetOwnersSelectList(housing.OwnerId);
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
            var currentUser = await _userManager.GetUserAsync(User);
            if (housing.OwnerId != currentUser?.Id && currentUser?.Role != "ADMIN")
            {
                return Forbid();
            }
            if (housing == null)
            {
                return NotFound();
            }

            return View(housing);
        }

        private async Task SyncHousingAvailabilityAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var occupiedHousingIds = await _context.BookingRequests
                .Where(b => b.DateFrom <= today && b.DateTo >= today)
                .Select(b => b.HousingId)
                .Distinct()
                .ToListAsync();

            var occupiedSet = occupiedHousingIds.ToHashSet();
            var housings = await _context.Housings.ToListAsync();
            var changed = false;

            foreach (var housing in housings)
            {
                var activeBookingsCount = occupiedHousingIds.Count(id => id == housing.Id);
                var shouldBeAvailable = housing.Rooms > activeBookingsCount; if (housing.IsAvailable != shouldBeAvailable)
                {
                    housing.IsAvailable = shouldBeAvailable;
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
        }
        private void SetOwnersSelectList(int? selectedOwnerId = null)
        {
            var owners = _context.Users
                .Where(u => u.WantsToBeOwner && u.IsOwnerApproved)
                .Select(u => new
                {
                    u.Id,
                    Display = $"{u.Name} ({u.Email})"
                })
                .ToList();

            ViewBag.OwnerId = new SelectList(owners, "Id", "Display", selectedOwnerId);
        }


        private void SetCitiesSelectList(string? selectedCity = null)
        {
            var cities = UkraineCities.ToList();

            if (!string.IsNullOrWhiteSpace(selectedCity) &&
                !cities.Contains(selectedCity, StringComparer.OrdinalIgnoreCase))
            {
                cities.Insert(0, selectedCity);
            }

            ViewBag.Cities = cities;
        }


        private static readonly string[] UkraineCities = new[] 
        {

            "Авдіївка", "Алмазна", "Алупка", "Алушта", "Алчевськ", "Амвросіївка",
            "Ананьїв", "Андрушівка", "Антрацит", "Апостолове", "Армянськ",
            "Арциз", "Балаклія", "Балта", "Бар", "Баранівка", "Барвінкове",
            "Батурин", "Бахмач", "Бахмут", "Бахчисарай", "Баштанка", "Белз",
            "Бердичів", "Бердянськ", "Берегове", "Бережани", "Березань", "Березівка",
            "Березне", "Берестечко", "Берестин", "Берислав", "Бершадь", "Бібрка",
            "Біла Церква", "Білгород-Дністровський", "Білицьке", "Білогірськ", "Білозерське",
            "Білопілля", "Біляївка", "Благовіщенське", "Бобринець", "Бобровиця", "Богодухів",
            "Богуслав", "Боково-Хрустальне", "Болград", "Болехів", "Борзна", "Борислав",
            "Бориспіль", "Борщів", "Боярка", "Бровари", "Броди", "Брянка", "Бунге", "Буринь",
            "Бурштин", "Буськ", "Буча", "Бучач", "Валки", "Вараш", "Василівка", "Васильків",
            "Ватутіне", "Вашківці", "Великі Мости", "Верхівцеве", "Верхньодніпровськ", "Вижниця",
            "Вилкове", "Винники", "Виноградів", "Вишгород", "Вишневе", "Вільногірськ",
            "Вільнянськ", "Вінниця", "Вовчанськ", "Вознесенівка", "Вознесенськ", "Волноваха",
            "Володимир", "Володимир-Волинський", "Волочиськ", "Ворожба", "Вуглегірськ",
            "Вугледар", "Гадяч", "Гайворон", "Гайсин", "Галич", "Генічеськ", "Герца", "Гірник",
            "Гірське", "Глиняни", "Глобине", "Глухів", "Гнівань", "Гола Пристань", "Голубівка",
            "Горішні Плавні", "Горлівка", "Городенка", "Городище", "Городня", "Городок", "Горохів",
            "Гребінка", "Гуляйполе", "Дебальцеве", "Деражня", "Дергачі", "Джанкой", "Дніпро",
            "Дніпрорудне", "Добромиль", "Добропілля", "Довжанськ", "Докучаєвськ", "Долина",
            "Долинська", "Донецьк", "Дрогобич", "Дружба", "Дружківка", "Дубляни", "Дубно",
            "Дубровиця", "Дунаївці", "Енергодар", "Євпаторія", "Єнакієве", "Жашків", "Жданівка",
            "Жидачів", "Житомир", "Жмеринка", "Жовква", "Жовті Води", "Заводське", "Залізне",
            "Заліщики", "Запоріжжя", "Заставна", "Збараж", "Зборів", "Звенигородка", "Звягель",
            "Здолбунів", "Зеленодольськ", "Зимогір'я", "Зіньків", "Зміїв", "Знам'янка", "Золоте",
            "Золотоноша", "Золочів", "Зоринськ", "Зугрес", "Івано-Франківськ", "Ізмаїл", "Ізюм",
            "Ізяслав", "Іллінці", "Іловайськ", "Інгулець", "Інкерман", "Ірміно", "Ірпінь", "Іршава",
            "Ічня", "Кагарлик", "Кадіївка", "Калинівка", "Калуш", "Кальміуське", "Кам'янець-Подільський",
            "Кам'янка", "Кам'янка-Бузька", "Кам'янка-Дніпровська", "Кам'янське", "Камінь-Каширський",
            "Канів", "Карлівка", "Каховка", "Керч", "Київ", "Кипуче", "Ківерці", "Кілія", "Кіцмань",
            "Кобеляки", "Ковель", "Кодима", "Карлівка", "Каховка", "Керч", "Київ", "Кипуче", "Ківерці",
            "Кілія", "Кіцмань", "Кобеляки", "Ковель", "Кодима", "Козятин", "Коломия", "Комарно", "Конотоп",
            "Копичинці", "Корець", "Коростень", "Коростишів", "Корсунь-Шевченківський", "Корюківка", "Косів",
            "Костопіль", "Костянтинівка", "Краматорськ", "Красилів", "Красногорівка", "Кременець", "Кременчук",
            "Кремінна", "Кривий Ріг", "Кролевець", "Кропивницький", "Куп'янськ", "Курахове", "Ладижин",
            "Ланівці", "Лебедин", "Лиман", "Липовець", "Лисичанськ", "Лозова", "Лохвиця", "Лубни",
            "Луганськ", "Лутугине", "Луцьк", "Львів", "Любомль", "Люботин", "Макіївка", "Мала Виска",
            "Малин", "Маневичі", "Мар'їнка", "Марганець", "Маріуполь", "Мелітополь", "Мена", "Мерефа",
            "Миколаїв", "Миколаївка", "Миргород", "Мирноград", "Миронівка", "Міусинськ", "Могилів-Подільський",
            "Молочанськ", "Монастириська", "Монастирище", "Моршин", "Моспине", "Мостиська", "Мукачево",
            "Надвірна", "Немирів", "Нетішин", "Ніжин", "Нікополь", "Нова Каховка", "Нова Одеса",
            "Новгород-Сіверський", "Нове", "Нове Давидково", "Новий Буг", "Новий Калинів", "Новий Розділ",
            "Новоазовськ", "Нововолинськ", "Новогродівка", "Новодністровськ", "Новодружеськ", "Новомиргород",
            "Новоселиця", "Новоукраїнка", "Новояворівськ", "Носівка", "Обухів", "Овруч", "Одеса", "Олевськ",
            "Олександрівськ", "Олександрія", "Олешки", "Олика", "Оріхів", "Остер", "Острог", "Отаманівка",
            "Охтирка", "Очаків", "П'ятихатки", "Павлоград", "Первомайськ", "Перевальськ", "Перемишляни",
            "Перечин", "Перещепине", "Переяслав", "Петрове", "Петрово-Красносілля", "Пирятин", "Південне",
            "Південноукраїнськ", "Підгайці", "Підгороднє", "Погребище", "Подільськ", "Покров", "Покровськ",
            "Полігон", "Пологи", "Полонне", "Полтава", "Помічна", "Попасна", "Попільня", "Почаїв", "Привілля",
            "Прилуки", "Приморськ", "Прип'ять", "Пустомити", "Путивль", "Рава-Руська", "Радехів", "Радивилів",
            "Радомишль", "Рахів", "Рені", "Решетилівка", "Ржищів", "Рівне", "Ровеньки", "Рогатин", "Родинське",
            "Рожище", "Роздільна", "Ромни", "Рубіжне", "Рудки", "Саки", "Самар", "Самбір", "Сарни", "Свалява",
            "Сватове", "Світловодськ", "Світлодарськ", "Святогірськ", "Севастополь", "Селидове", "Семенівка",
            "Середина-Буда", "Синельникове", "Сіверськ", "Сіверськодонецьк", "Сімферополь", "Скадовськ", "Скалат",
            "Сквира", "Сколе", "Славута", "Славутич", "Слобожанське", "Слов'янськ", "Сміла", "Снігурівка", "Сніжне",
            "Сновськ", "Снятин", "Сокаль", "Сокиряни", "Сокологірськ", "Соледар", "Сорокине", "Соснівка", "Старий Крим",
            "Старий Самбір", "Старобільськ", "Старокостянтинів", "Стебник", "Степове", "Сторожинець", "Стрий", "Стрілиця",
            "Судак", "Судова Вишня", "Суми", "Суходільськ", "Таврійськ", "Тальне", "Тараща", "Татарбунари", "Теплодар",
            "Теребовля", "Тернівка", "Тернопіль", "Тетіїв", "Тисмениця", "Тихе", "Тлумач", "Токмак", "Томашгород",
            "Торецьк", "Тростянець", "Трускавець", "Тульчин", "Турка", "Тячів", "Угнів", "Ужгород", "Узин", "Українка",
            "Українськ", "Умань", "Устилуг", "Фастів", "Федорівка", "Феодосія", "Харків", "Харцизьк", "Херсон",
            "Хирів", "Хмельницький", "Хмільник", "Ходорів", "Хорол", "Хоростків", "Хотин", "Хрестівка", "Христинівка",
            "Хрустальний", "Хуст", "Хутір-Михайлівський", "Часів Яр", "Червоносів", "Черкаси", "Чернівці",
            "Чернігів", "Чигирин", "Чистякове", "Чоп", "Чорнобиль", "Чорноморськ", "Чортків", "Чугуїв",
            "Чуднів", "Шаргород", "Шахтарськ", "Шахтарське", "Шепетівка", "Шептицький", "Шостка", "Шпола",
            "Шумськ", "Щастя", "Щолкіне", "Яворів", "Яготин", "Ялта", "Ямпіль", "Яни Капу", "Яремче", "Ясинувата"
        };


        // POST: Housings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var housing = await _context.Housings.FindAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);
            if (housing.OwnerId != currentUser?.Id && currentUser?.Role != "ADMIN")
            {
                return Forbid();
            }
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
