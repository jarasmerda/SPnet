using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestAPI1.Endpoints;

public static class NextCode
{
    public static IEndpointRouteBuilder MapCpqNextCode(this IEndpointRouteBuilder app)
    {
        app.MapGet("/CpqNextCode", async (BomDb db) =>
        {
            try
            {
                // Načtení všech relevantních IDS
                var idsList = await db.SKz
                    .Where(r => r.StockCat == "SHEET PE" &&
                                !string.IsNullOrWhiteSpace(r.IDS))
                    .Select(r => r.IDS)
                    .ToListAsync();

                int maxId = 0;

                foreach (var rawId in idsList)
                {
                    if (string.IsNullOrWhiteSpace(rawId)) continue;

                    // vezmeme poslední 4 znaky (pokud existují)
                    string lastFour = rawId.Length >= 4
                        ? rawId[^4..].Trim()
                        : rawId.Trim();

                    if (int.TryParse(lastFour, out int num) && num > maxId)
                    {
                        maxId = num;
                    }
                }

                int nextNumber = maxId + 1;
                string nextCode = $"S{nextNumber:D4}";

                Console.WriteLine($"[nextcode] Načteno {idsList.Count} HDPE karet → další kód: {nextCode}");

                return Results.Json(new { code = nextCode });
            }
            catch (Exception ex)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("CHYBA v endpointu GET /nextcode");
                Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Zpráva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("=====================================");

                return Results.Problem(
                    detail: "Došlo k chybě při generování dalšího kódu. Zkuste to později.",
                    statusCode: 500,
                    title: "Interní chyba serveru"
                );
            }
        })
        .WithName("CpqNextCode");

        return app;
    }
}