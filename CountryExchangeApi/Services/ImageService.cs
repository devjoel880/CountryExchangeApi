using CountryExchangeApi.Models;
using Microsoft.Extensions.Configuration;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CountryExchangeApi.Services
{
    public class ImageService
    {
        private readonly IConfiguration _config;
        public ImageService(IConfiguration config) { _config = config; }

        public async Task GenerateSummaryImageAsync(int totalCountries, List<Country> top5, DateTime timestampUtc)
        {
            var path = _config["CacheImagePath"] ?? "cache/summary.png";
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            int width = 1200, height = 600;
            using var image = new Image<Rgba32>(width, height);
            image.Mutate(ctx =>
            {
                ctx.Fill(Color.White);
                var titleFont = SystemFonts.CreateFont("Arial", 36);
                var textFont = SystemFonts.CreateFont("Arial", 20);

                ctx.DrawText("Countries Summary", titleFont, Color.Black, new PointF(20, 20));
                ctx.DrawText($"Total countries: {totalCountries}", textFont, Color.Black, new PointF(20, 80));
                ctx.DrawText($"Last refresh (UTC): {timestampUtc:u}", textFont, Color.Black, new PointF(20, 120));

                ctx.DrawText("Top 5 by Estimated GDP:", textFont, Color.Black, new PointF(20, 170));
                float y = 210f;
                int rank = 1;
                foreach (var c in top5)
                {
                    var gdpText = c.EstimatedGdp.HasValue ? $"{c.EstimatedGdp.Value:N2}" : "N/A";
                    ctx.DrawText($"{rank}. {c.Name} — {gdpText}", textFont, Color.Black, new PointF(20, y));
                    y += 34f;
                    rank++;
                }
            });

            await image.SaveAsPngAsync(path);
        }
    }
}
