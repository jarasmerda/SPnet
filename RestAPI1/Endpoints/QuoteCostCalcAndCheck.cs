using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System.Collections.Generic;
using System.Linq;

namespace RestAPI1.Endpoints
{
    public static class BomGenerateEndpoints
    {
        public static IEndpointRouteBuilder MapQuoteCostCalcAndCheck(this IEndpointRouteBuilder app)
        {
            // POST /generate-bom-routing – generování BOM a routing s kontrolou typů a cen
            app.MapPost("/QuoteCostCalcAndCheck", async (BomDb db, BomRequest request) =>
            {
                if (request == null || request.items == null || request.items.Count == 0)
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
                        .Where(r => r.ProductNumber == code)
                        .ToListAsync();

                    var foundTypes = bomRows
                        .Where(r => !string.IsNullOrEmpty(r.Type))
                        .Select(r => r.Type!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToHashSet();

                    var missingTypes = requiredTypes
                        .Where(rt => !foundTypes.Contains(rt))
                        .ToList();

                    bool hasAllRequiredTypes = missingTypes.Count == 0;

                    var missingPrices = new List<string>();
                    decimal totalCost = 0m;

                    foreach (var row in bomRows)
                    {
                        if (!row.Qty.HasValue || row.Qty.Value <= 0)
                            continue;

                        string matNum = row.MaterialNumber?.Trim() ?? "";

                        decimal cenaJednotky = 0m;

                        if (!string.IsNullOrEmpty(matNum))
                        {
                            cenaJednotky = await db.SKz
                                .Where(p => p.IDS == matNum)
                                .Select(p => p.PURCHASE_PRICE ?? 0m)
                                .FirstOrDefaultAsync();

                            if (cenaJednotky > 0)
                            {
                                totalCost += row.Qty.Value * cenaJednotky;
                            }
                            else
                            {
                                missingPrices.Add(matNum + " (nenalezeno v SKz nebo cena = 0)");
                            }
                        }
                        else
                        {
                            missingPrices.Add("bez material #");
                        }
                    }

                    bool priceComplete = missingPrices.Count == 0;
                    bool bomComplete = hasAllRequiredTypes && priceComplete;

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
            .WithName("QuoteCostCalcAndCheck");

            // případně další BOM-related endpointy sem...

            return app;
        }
    }
}