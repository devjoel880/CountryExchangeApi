using System.ComponentModel.DataAnnotations;

namespace CountryExchangeApi.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Capital { get; set; }

        public string? Region { get; set; }

        [Required]
        public long Population { get; set; }

        // can be null if no currencies
        public string? CurrencyCode { get; set; }

        public decimal? ExchangeRate { get; set; }

        public decimal? EstimatedGdp { get; set; }

        public string? FlagUrl { get; set; }

        public DateTime LastRefreshedAt { get; set; }
    }
}
