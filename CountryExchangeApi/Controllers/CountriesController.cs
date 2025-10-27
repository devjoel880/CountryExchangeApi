using System.Globalization;
using CountryExchangeApi.Data;
using CountryExchangeApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountryExchangeApi.Controllers
{
    [ApiController]
    [Route("countries")]
    public class CountriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ExternalDataService _external;
        private readonly IConfiguration _config;

        public CountriesController(AppDbContext db, ExternalDataService external, IConfiguration config)
        {
            _db = db;
            _external = external;
            _config = config;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var (ok, errorApi) = await _external.RefreshAllAsync();
            if (!ok)
            {
                if (errorApi == "Countries API" || errorApi == "Exchange Rates API")
                {
                    return StatusCode(503, new { error = "External data source unavailable", details = $"Could not fetch data from {errorApi}" });
                }
                return StatusCode(503, new { error = "External data source unavailable", details = errorApi });
            }

            var last = await _db.Countries.MaxAsync(c => c.LastRefreshedAt);
            return Ok(new { message = "Refresh completed", last_refreshed_at = last.ToUniversalTime().ToString("o") });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? region, [FromQuery] string? currency, [FromQuery] string? sort)
        {
            var q = _db.Countries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(region))
                q = q.Where(c => c.Region == region);

            if (!string.IsNullOrWhiteSpace(currency))
            {
                var up = currency.ToUpperInvariant();
                q = q.Where(c => c.CurrencyCode != null && c.CurrencyCode.ToUpper() == up);
            }

            if (!string.IsNullOrWhiteSpace(sort))
            {
                if (sort == "gdp_desc") q = q.OrderByDescending(c => c.EstimatedGdp);
                else if (sort == "gdp_asc") q = q.OrderBy(c => c.EstimatedGdp);
                else if (sort == "name_desc") q = q.OrderByDescending(c => c.Name);
                else q = q.OrderBy(c => c.Name);
            }
            else q = q.OrderBy(c => c.Id);

            var list = await q.ToListAsync();

            var result = list.Select(c => new {
                id = c.Id,
                name = c.Name,
                capital = c.Capital,
                region = c.Region,
                population = c.Population,
                currency_code = c.CurrencyCode,
                exchange_rate = c.ExchangeRate,
                estimated_gdp = c.EstimatedGdp,
                flag_url = c.FlagUrl,
                last_refreshed_at = c.LastRefreshedAt.ToUniversalTime().ToString("o")
            });

            return Ok(result);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetOne(string name)
        {
            var country = await _db.Countries.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
            if (country == null) return NotFound(new { error = "Country not found" });

            return Ok(new
            {
                id = country.Id,
                name = country.Name,
                capital = country.Capital,
                region = country.Region,
                population = country.Population,
                currency_code = country.CurrencyCode,
                exchange_rate = country.ExchangeRate,
                estimated_gdp = country.EstimatedGdp,
                flag_url = country.FlagUrl,
                last_refreshed_at = country.LastRefreshedAt.ToUniversalTime().ToString("o")
            });
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var country = await _db.Countries.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
            if (country == null) return NotFound(new { error = "Country not found" });

            _db.Countries.Remove(country);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Deleted", name = country.Name });
        }

        [HttpGet("image")]
        public IActionResult GetImage()
        {
            var path = _config["CacheImagePath"] ?? "cache/summary.png";
            if (!System.IO.File.Exists(path))
                return NotFound(new { error = "Summary image not found" });

            var bytes = System.IO.File.ReadAllBytes(path);
            return File(bytes, "image/png");
        }
    }
}
