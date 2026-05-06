using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestAPI1.Endpoints;

public static class CrmCustomers
{
    public static IEndpointRouteBuilder MapQuoteCustomers(this IEndpointRouteBuilder app)
    {
        app.MapGet("QuoteCustomers", async (BomDb db) =>
        {
            try
            {
                // Načtení relevantních zákazníků
                var rawCustomers = await db.Customers
                    .Where(a =>
                        !string.IsNullOrWhiteSpace(a.Firma) ||
                        (!string.IsNullOrWhiteSpace(a.Jmeno) && !string.IsNullOrWhiteSpace(a.Jmeno2)))
                    .OrderBy(a => a.Firma)
                    .ThenBy(a => a.Jmeno2)
                    .ThenBy(a => a.Jmeno)
                    .Select(a => new
                    {
                        a.ID,
                        a.Firma,
                        a.Firma2,
                        a.Jmeno,
                        a.Jmeno2
                    })
                    .Take(1500)
                    .ToListAsync();

                // Transformace na finální formát
                var customers = rawCustomers
                    .Select(a => new
                    {
                        id = a.ID.ToString().PadLeft(6, '0'),
                        name = CreateCustomerName(a.Firma, a.Firma2, a.Jmeno, a.Jmeno2)
                    })
                    .Where(c => !string.IsNullOrWhiteSpace(c.name))
                    .OrderBy(c => c.name)
                    .ToList();

                Console.WriteLine($"[/crm/customers] Načteno {customers.Count} zákazníků");

                return Results.Json(customers);
            }
            catch (Exception ex)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("CHYBA v endpointu GET /crm/customers");
                Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Zpráva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("=====================================");

                return Results.Problem(
                    detail: "Došlo k chybě při načítání seznamu zákazníků. Zkuste to později.",
                    statusCode: 500,
                    title: "Interní chyba serveru"
                );
            }
        })
        .WithName("QuoteCustomers");

        return app;
    }

    // Pomocná funkce – stejná jako ve staré verzi
    private static string CreateCustomerName(string? firma, string? firma2, string? jmeno, string? jmeno2)
    {
        if (!string.IsNullOrWhiteSpace(firma))
        {
            string baseName = firma.Trim();
            if (!string.IsNullOrWhiteSpace(firma2))
                baseName += " " + firma2.Trim();
            return baseName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(jmeno) && !string.IsNullOrWhiteSpace(jmeno2))
        {
            return $"{jmeno2.Trim()} {jmeno.Trim()}";
        }

        return "Bez názvu";
    }
}