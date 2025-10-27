using CountryExchangeApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CountryExchangeApi.Controllers
{
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly AppDbContext _db;
        public StatusController(AppDbContext db) { _db = db; }

        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var total = await _db.Countries.CountAsync();
            var last = await _db.Countries.MaxAsync(c => (DateTime?)c.LastRefreshedAt);
            return Ok(new { total_countries = total, last_refreshed_at = last?.ToUniversalTime().ToString("o") });
        }
    }
}
