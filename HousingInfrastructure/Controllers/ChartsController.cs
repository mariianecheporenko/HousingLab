using HousingInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousingInfrastructure.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChartsController : ControllerBase
{
    private readonly HousingContext _context;

    public ChartsController(HousingContext context)
    {
        _context = context;
    }

    public record CityHousingCountResponseItem(string City, int Count);

    public record PriceCategoryAverageRatingResponseItem(string Category, double AverageRating);

    [HttpGet("housingCountByCity")]
    public async Task<JsonResult> GetHousingCountByCityAsync(CancellationToken cancellationToken)
    {
        var rawItems = await _context.Housings
            .AsNoTracking()
            .GroupBy(h => h.City)
            .Select(group => new CityHousingCountResponseItem(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var responseItems = rawItems
            .GroupBy(item => string.IsNullOrWhiteSpace(item.City) ? "Місто не вказано" : item.City.Trim())
            .Select(group => new CityHousingCountResponseItem(group.Key, group.Sum(item => item.Count)))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.City)
            .ToList();

        return new JsonResult(responseItems);
    }

    [HttpGet("averageRatingByPriceCategory")]
    public async Task<JsonResult> GetAverageRatingByPriceCategoryAsync(CancellationToken cancellationToken)
    {
        var rawResponseItems = await _context.Reviews
            .AsNoTracking()
            .Join(
                _context.Housings.AsNoTracking().Where(h => h.Price.HasValue),
                review => review.HousingId,
                housing => housing.Id,
                (review, housing) => new
                {
                    Price = housing.Price!.Value,
                    review.Rating
                })
            .GroupBy(item => item.Price <= 10000m
                ? "Бюджетне"
                : item.Price <= 20000m
                    ? "Середнє"
                    : "Преміум")
            .Select(group => new PriceCategoryAverageRatingResponseItem(
                group.Key,
                Math.Round(group.Average(x => x.Rating), 2)))
            .ToListAsync(cancellationToken);

        var orderedCategories = new[] { "Бюджетне", "Середнє", "Преміум" };
        var responseItems = orderedCategories
            .Select(category => rawResponseItems.FirstOrDefault(item => item.Category == category)
                ?? new PriceCategoryAverageRatingResponseItem(category, 0))
            .ToList();

        return new JsonResult(responseItems);
    }
}
