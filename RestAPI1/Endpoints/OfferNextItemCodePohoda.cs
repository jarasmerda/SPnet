using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints
{
    public static class PohodaNextVariantCodeEndpoint
    {
        public static IEndpointRouteBuilder MapOfferNextItemCodePohoda(this IEndpointRouteBuilder app)
        {
            // GET /pohoda/next-variant-code?baseCode=S0059
            // -> { "code": "S0059-01" } nebo { "code": "S0059-08" }
            app.MapGet("OfferNextItemCodePohoda", async (BomDb db, string baseCode) =>
            {
                baseCode = (baseCode ?? "").Trim();

                if (string.IsNullOrWhiteSpace(baseCode))
                    return Results.BadRequest(new { error = "Missing baseCode. Example: ?baseCode=S0059" });

                // když někdo omylem pošle "S0059-03", vezmeme jen base část
                var dashIdx = baseCode.IndexOf('-');
                if (dashIdx > 0) baseCode = baseCode.Substring(0, dashIdx).Trim();

                var prefix = baseCode + "-";

                // 1) V SQL jen vyfiltrujeme prefix, parsing čísla uděláme až v paměti (bez problémů s EF překladem).
                var idsList = await db.SKz
                    .AsNoTracking()
                    .Where(s => s.IDS != null && s.IDS.StartsWith(prefix))
                    .Select(s => s.IDS!)
                    .ToListAsync();

                // 2) Najdi max suffix (číslo)
                var max = 0;
                foreach (var ids in idsList)
                {
                    // očekáváme formát: S0059-yy (yy = číslo)
                    var suffix = ids.Substring(prefix.Length).Trim();

                    // když jsou v IDS ještě další znaky (např. S0059-01A), ignorujeme
                    if (!int.TryParse(suffix, out var n))
                        continue;

                    if (n > max) max = n;
                }

                var next = max + 1;

                // 2-ciferné formátování (01, 02, ... 99). Pokud přeroste 99, .ToString("D2") vrátí "100" atd.
                var nextCode = $"{baseCode}-{next:D2}";

                return Results.Json(new { code = nextCode });
            })
            .WithName("OfferNextItemCodePohoda");

            return app;
        }
    }
}
