using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestAPI1.Endpoints;

public static class Check
{
    public static IEndpointRouteBuilder MapCpqItemPohodaCheck(this IEndpointRouteBuilder app)
    {
        app.MapGet("/CpqItemPohodaCheck", async (BomDb db, HttpContext context) =>
        {
            try
            {
                // 1. Načtení vybraných atributů z query
                var selected = new Dictionary<int, string>(capacity: 7);

                for (int i = 1; i <= 7; i++)
                {
                    var val = context.Request.Query[$"attr{i}"].ToString().Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        selected[i] = val;
                    }
                }

                // 2. Pokud není všech 7 atributů → rychlá odpověď
                if (selected.Count < 7)
                {
                    return Results.Json(new
                    {
                        exists = false,
                        code = (string?)null,
                        message = "Vyplňte všechny atributy pro ověření existence v Pohodě."
                    });
                }

                // 3. Hledání shody v tabulce SKz
                var matchingItem = await db.SKz
                    .Where(r =>
                        r.VPrAttr1 == selected[1] &&
                        r.VPrAttr2 == selected[2] &&
                        r.VPrAttr3 == selected[3] &&
                        r.VPrAttr4 == selected[4] &&
                        r.VPrAttr5 == selected[5] &&
                        r.VPrAttr6 == selected[6] &&
                        r.VPrAttr7 == selected[7])
                    .Select(r => r.IDS)
                    .FirstOrDefaultAsync();

                bool exists = matchingItem != null;
                string code = matchingItem?.Trim() ?? "";

                string message = exists
                    ? $"STATUS: ✓ ALREADY CREATED IN POHODA (ITEM: {code})"
                    : "NEW ITEM WILL BE CREATED IN POHODA.";

                return Results.Json(new
                {
                    exists,
                    code,
                    message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("CHYBA v endpointu GET /check");
                Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Zpráva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("=====================================");

                return Results.Problem(
                    detail: "Došlo k chybě při kontrole existence v Pohodě. Zkuste to později.",
                    statusCode: 500,
                    title: "Interní chyba serveru"
                );
            }
        })
        .WithName("CpqItemPohodaCheck");

        return app;
    }
}