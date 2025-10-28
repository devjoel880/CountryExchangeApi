using CountryExchangeApi.Data;
using CountryExchangeApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CountryExchangeApi.Services
{
    public class ExternalDataService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ImageService _imageService;

        public ExternalDataService(IHttpClientFactory httpFactory, AppDbContext db, IConfiguration config, ImageService imageService)
        {
            _httpFactory = httpFactory;
            _db = db;
            _config = config;
            _imageService = imageService;
        }

        public async Task<(bool ok, string? errorApiName)> RefreshAllAsync()
        {
            var client = _httpFactory.CreateClient("external");
            client.Timeout = TimeSpan.FromSeconds(30);

            string countriesUrl = _config["ExternalApis:CountriesUrl"]!;
            string ratesUrl = _config["ExternalApis:ExchangeRatesUrl"]!;

            JArray countriesArray;
            JObject ratesJson;

            // fetch countries
            try
            {
                var countriesResp = await client.GetStringAsync(countriesUrl);
                countriesArray = JArray.Parse(countriesResp);
            }
            catch
            {
                return (false, "Countries API");
            }

            // fetch rates
            try
            {
                var ratesResp = await client.GetStringAsync(ratesUrl);
                ratesJson = JObject.Parse(ratesResp);
            }
            catch
            {
                return (false, "Exchange Rates API");
            }

            var ratesToken = ratesJson["rates"];
            var ratesDict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (ratesToken is JObject ratesObj)
            {
                foreach (var prop in ratesObj.Properties())
                {
                    if (decimal.TryParse(prop.Value.ToString(), out var val))
                        ratesDict[prop.Name] = val;
                }
            }

            try
            {
                var now = DateTime.UtcNow;

                foreach (var c in countriesArray)
                {
                    var name = c["name"]?.ToString();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var capital = c["capital"]?.ToString();
                    var region = c["region"]?.ToString();
                    var population = c["population"]?.ToObject<long?>() ?? 0L;
                    var flag = c["flag"]?.ToString();

                    string? currencyCode = null;
                    var currencies = c["currencies"] as JArray;
                    if (currencies != null && currencies.Count > 0)
                    {
                        var first = currencies.First as JObject;
                        currencyCode = first?["code"]?.ToString();
                        if (string.IsNullOrWhiteSpace(currencyCode)) currencyCode = null;
                    }

                    decimal? exchangeRate = null;
                    decimal? estimatedGdp = null;

                    if (currencyCode == null)
                    {
                        estimatedGdp = 0m;
                    }
                    else
                    {
                        if (ratesDict.TryGetValue(currencyCode, out var rate))
                        {
                            exchangeRate = rate;
                            var multiplier = Random.Shared.Next(1000, 2001);
                            estimatedGdp = rate == 0 ? null : (decimal)population * multiplier / rate;
                        }
                    }

                    var existing = await _db.Countries.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

                    if (existing != null)
                    {
                        existing.Capital = capital;
                        existing.Region = region;
                        existing.Population = population;
                        existing.CurrencyCode = currencyCode;
                        existing.ExchangeRate = exchangeRate;
                        existing.EstimatedGdp = estimatedGdp;
                        existing.FlagUrl = flag;
                        existing.LastRefreshedAt = now;
                        _db.Countries.Update(existing);
                    }
                    else
                    {
                        var entity = new Country
                        {
                            Name = name,
                            Capital = capital,
                            Region = region,
                            Population = population,
                            CurrencyCode = currencyCode,
                            ExchangeRate = exchangeRate,
                            EstimatedGdp = estimatedGdp,
                            FlagUrl = flag,
                            LastRefreshedAt = now
                        };
                        await _db.Countries.AddAsync(entity);
                    }
                }

                await _db.SaveChangesAsync();

                // Now safely query again after saving
                var totalCountries = await _db.Countries.CountAsync();
                var top5 = await _db.Countries
                    .Where(c => c.EstimatedGdp != null)
                    .OrderByDescending(c => c.EstimatedGdp)
                    .Take(5)
                    .ToListAsync();

                await _imageService.GenerateSummaryImageAsync(totalCountries, top5, now);

                return (true, null);
            }
            catch (Exception)
            {
                return (false, "Internal processing during refresh");
            }
        }


        //public async Task<(bool ok, string? errorApiName)> RefreshAllAsync()
        //{
        //    var client = _httpFactory.CreateClient("external");
        //    client.Timeout = TimeSpan.FromSeconds(30);

        //    string countriesUrl = _config["ExternalApis:CountriesUrl"]!;
        //    string ratesUrl = _config["ExternalApis:ExchangeRatesUrl"]!;

        //    JArray countriesArray;
        //    JObject ratesJson;

        //    // fetch countries
        //    try
        //    {
        //        var countriesResp = await client.GetStringAsync(countriesUrl);
        //        countriesArray = JArray.Parse(countriesResp);
        //    }
        //    catch
        //    {
        //        return (false, "Countries API");
        //    }

        //    // fetch rates
        //    try
        //    {
        //        var ratesResp = await client.GetStringAsync(ratesUrl);
        //        ratesJson = JObject.Parse(ratesResp);
        //    }
        //    catch
        //    {
        //        return (false, "Exchange Rates API");
        //    }

        //    var ratesToken = ratesJson["rates"];
        //    var ratesDict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        //    if (ratesToken is JObject ratesObj)
        //    {
        //        foreach (var prop in ratesObj.Properties())
        //        {
        //            if (decimal.TryParse(prop.Value.ToString(), out var val))
        //                ratesDict[prop.Name] = val;
        //        }
        //    }

        //    using var tx = await _db.Database.BeginTransactionAsync();

        //    try
        //    {
        //        var now = DateTime.UtcNow;

        //        foreach (var c in countriesArray)
        //        {
        //            var name = c["name"]?.ToString();
        //            if (string.IsNullOrWhiteSpace(name)) continue;

        //            var capital = c["capital"]?.ToString();
        //            var region = c["region"]?.ToString();
        //            var population = c["population"]?.ToObject<long?>() ?? 0L;
        //            var flag = c["flag"]?.ToString();

        //            string? currencyCode = null;
        //            var currencies = c["currencies"] as JArray;
        //            if (currencies != null && currencies.Count > 0)
        //            {
        //                var first = currencies.First as JObject;
        //                currencyCode = first?["code"]?.ToString();
        //                if (string.IsNullOrWhiteSpace(currencyCode)) currencyCode = null;
        //            }

        //            decimal? exchangeRate = null;
        //            decimal? estimatedGdp = null;

        //            if (currencyCode == null)
        //            {
        //                // per spec: currency missing -> estimated_gdp = 0, exchange_rate null
        //                estimatedGdp = 0m;
        //            }
        //            else
        //            {
        //                if (ratesDict.TryGetValue(currencyCode, out var rate))
        //                {
        //                    exchangeRate = rate;

        //                    //var rng = new Random();
        //                    //var multiplier = rng.Next(1000, 2001); // Since this is inside a loop, repeated sequences could occur

        //                    var multiplier = Random.Shared.Next(1000, 2001);
        //                    if (exchangeRate == 0) estimatedGdp = null;
        //                    else estimatedGdp = (decimal)population * multiplier / exchangeRate;
        //                }
        //                else
        //                {
        //                    // currency not in rates
        //                    exchangeRate = null;
        //                    estimatedGdp = null;
        //                }
        //            }

        //            // upsert by name (case-insensitive)
        //            var existing = await _db.Countries.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

        //            if (existing != null)
        //            {
        //                existing.Capital = capital;
        //                existing.Region = region;
        //                existing.Population = population;
        //                existing.CurrencyCode = currencyCode;
        //                existing.ExchangeRate = exchangeRate;
        //                existing.EstimatedGdp = estimatedGdp;
        //                existing.FlagUrl = flag;
        //                existing.LastRefreshedAt = now;
        //                _db.Countries.Update(existing);
        //            }
        //            else
        //            {
        //                var entity = new Country
        //                {
        //                    Name = name,
        //                    Capital = capital,
        //                    Region = region,
        //                    Population = population,
        //                    CurrencyCode = currencyCode,
        //                    ExchangeRate = exchangeRate,
        //                    EstimatedGdp = estimatedGdp,
        //                    FlagUrl = flag,
        //                    LastRefreshedAt = now
        //                };
        //                await _db.Countries.AddAsync(entity);
        //            }
        //        }

        //        await _db.SaveChangesAsync();
        //        await tx.CommitAsync();

        //        // After commit, generate summary image
        //        var totalCountries = await _db.Countries.CountAsync();
        //        var top5 = await _db.Countries
        //            .Where(c => c.EstimatedGdp != null)
        //            .OrderByDescending(c => c.EstimatedGdp)
        //            .Take(5)
        //            .ToListAsync();

        //        await _imageService.GenerateSummaryImageAsync(totalCountries, top5, now);

        //        return (true, null);
        //    }
        //    catch
        //    {
        //        await tx.RollbackAsync();
        //        return (false, "Internal processing during refresh");
        //    }
        //}
    }
}
