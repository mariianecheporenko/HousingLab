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
            await SyncHousingAvailabilityAsync();
            var housingContext = _context.Housings.Include(h => h.Owner);
            return View(await housingContext.ToListAsync());
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
        public IActionResult Create()
        {
            SetCitiesSelectList();
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
            SetCitiesSelectList(housing.City);
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
            SetCitiesSelectList(housing.City);
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
            SetCitiesSelectList(housing.City);
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
                var shouldBeAvailable = !occupiedSet.Contains(housing.Id);
                if (housing.IsAvailable != shouldBeAvailable)
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
