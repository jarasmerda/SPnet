using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestAPI1.Endpoints;

public static class Filter
{
    public static IEndpointRouteBuilder MapCpqFilterValues(this IEndpointRouteBuilder app)
    {
        app.MapGet("/CpqFilterValues", async (BomDb db, HttpContext context) =>
        {
            try
            {
                // 1. Vybrané hodnoty z query stringu
                var selected = new Dictionary<int, string>(capacity: 7);

                for (int i = 1; i <= 7; i++)
                {
                    var val = context.Request.Query[$"attr{i}"].ToString().Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        selected[i] = val;
                    }
                }

                // 2. Všechny dostupné hodnoty pro každý atribut
                var available = new Dictionary<int, List<string>>(capacity: 7);

                for (int i = 1; i <= 7; i++)
                {
                    var values = await db.AttributeValues
    .Where(r => r.AttributeNumber == i && !string.IsNullOrWhiteSpace(r.AttributeValue))
    .Select(r => r.AttributeValue!.Trim())
    .Distinct()
    .OrderBy(v => v)
    .ToListAsync();

                    available[i] = values;
                }

                // 3. Načtení a aplikace pravidel
                var rules = await db.AttributesRules.ToListAsync();

                foreach (var rule in rules)
                {
                    if (selected.TryGetValue(rule.FromAttribute, out var selectedValue) &&
                        string.Equals(selectedValue, rule.FromValue, StringComparison.OrdinalIgnoreCase))
                    {
                        var allowed = rule.AllowedValues
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList();

                        if (available.TryGetValue(rule.ToAttribute, out var currentList))
                        {
                            available[rule.ToAttribute] = currentList
                                .Intersect(allowed, StringComparer.OrdinalIgnoreCase)
                                .ToList();
                        }
                    }
                }

                // 4. Výstup – klíče jako string ("1", "2", ...)
                var result = available.ToDictionary(
                    kv => kv.Key.ToString(),
                    kv => kv.Value ?? new List<string>()
                );

                return Results.Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("CHYBA v endpointu GET /filter");
                Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Zpráva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
                Console.WriteLine("=====================================");

                return Results.Problem(
                    detail: "Došlo k chybě při načítání filtrů. Zkuste to později.",
                    statusCode: 500,
                    title: "Interní chyba serveru"
                );
            }
        })
        .WithName("CpqFilterValues");

        return app;
    }
}