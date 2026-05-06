using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints;

public record BomRequest(List<BomRequestItem> items);
public record BomRequestItem(string code, int quantity, string attr1, string attr2, string attr3, string attr4, string attr5, string attr6, string attr7);

public static class BomGenerateRoutingEndpoints
{
    public static IEndpointRouteBuilder MapBomGenerateRouting(this IEndpointRouteBuilder app)
    {
        // POST /generate-bom-routing – zkontroluje BOM kompletnost a spočítá nákladovou cenu
        app.MapPost("/generate-bom-routing", async (BomDb db, BomRequest request) =>
        {
            if (request?.items == null || request.items.Count == 0)
                return Results.Json(new { success = false, message = "Žádné položky" }, statusCode: 400);

            var itemResults = new List<object>();
            var requiredTypes = new[] { "B_1", "B_2", "B_3", "B_4", "B_5" };

            foreach (var reqItem in request.items)
            {
                string code = reqItem.code?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(code))
                {
                    itemResults.Add(new
                    {
                        code = "???",
                        quantity = reqItem.quantity,
                        bomComplete = false,
                        priceComplete = false,
                        costPerPiece = 0m,
                        totalCost = 0m,
                        missingPrices = new[] { "Chybí kód" },
                        missingTypes = requiredTypes
                    });
                    continue;
                }

                var bomRows = await db.BomRouting
                    .Where(r => r.ProductNumber.Trim() == code)
                    .ToListAsync();

                var foundTypes = bomRows
                    .Where(r => !string.IsNullOrEmpty(r.Type))
                    .Select(r => r.Type!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet();

                var missingTypes = requiredTypes
                    .Where(rt => !foundTypes.Contains(rt))
                    .ToList();

                var missingPrices = new List<string>();
                decimal totalCost = 0m;

                foreach (var row in bomRows)
                {
                    if (!row.Qty.HasValue || row.Qty.Value <= 0)
                        continue;

                    string matNum = row.MaterialNumber?.Trim() ?? "";

                    if (!string.IsNullOrEmpty(matNum))
                    {
                        var cenaJednotky = await db.SKz
                            .Where(p => p.IDS == matNum)
                            .Select(p => p.PURCHASE_PRICE)
                            .FirstOrDefaultAsync();

                        if (cenaJednotky.HasValue && cenaJednotky.Value > 0)
                            totalCost += row.Qty.Value * cenaJednotky.Value;
                        else
                            missingPrices.Add(matNum + " (nenalezeno v SKz nebo cena = 0)");
                    }
                    else
                    {
                        missingPrices.Add("bez material #");
                    }
                }

                bool priceComplete = missingPrices.Count == 0;
                bool bomComplete = missingTypes.Count == 0 && priceComplete;
                decimal costPerPiece = reqItem.quantity > 0 ? totalCost / reqItem.quantity : 0m;

                itemResults.Add(new
                {
                    code,
                    quantity = reqItem.quantity,
                    bomComplete,
                    priceComplete,
                    costPerPiece,
                    totalCost,
                    missingPrices,
                    missingTypes,
                    foundTypesCount = foundTypes.Count
                });
            }

            return Results.Json(new
            {
                success = true,
                message = itemResults.All(r => ((dynamic)r).bomComplete) ? "BOM kompletní" : "BOM nekompletní",
                items = itemResults
            });
        })
        .WithName("GenerateBomRouting");

        // GET /lookup-item – vyhledá položku v dbo.SKz podle kódu
        app.MapGet("/lookup-item", async (BomDb db, string code) =>
        {
            if (string.IsNullOrWhiteSpace(code))
                return Results.BadRequest(new { found = false, message = "Chybí kód" });

            code = code.Trim();

            var record = await db.SKz
                .Where(p => p.IDS == code)
                .Select(p => new { p.IDS, p.DESCRIPTION })
                .FirstOrDefaultAsync();

            if (record != null)
            {
                return Results.Ok(new
                {
                    found = true,
                    source = "SKz",
                    code = record.IDS,
                    name = record.DESCRIPTION?.Trim() ?? $"HDPE položka {record.IDS}",
                    attr1 = "",
                    attr2 = "",
                    attr3 = "",
                    attr4 = "",
                    attr5 = "",
                    attr6 = "",
                    attr7 = "",
                    defaultQuantity = 1
                });
            }

            return Results.Ok(new
            {
                found = false,
                message = $"Kód '{code}' nebyl nalezen v tabulce dbo.SKz"
            });
        })
        .WithName("LookupItem");

        return app;
    }
}
