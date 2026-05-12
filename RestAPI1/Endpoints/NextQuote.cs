using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints
{
    public static class NextQuoteEndpoints
    {
        public static IEndpointRouteBuilder MapNextQuote(this IEndpointRouteBuilder app)
        {
            // GET /next-quote – vrátí další volné číslo nabídky (Q26xxxx)
            app.MapGet("/next-quote", async (BomDb db) =>
            {
                try
                {
                    // Revize (Q260063-01) jsou záměrně vyloučeny – jejich suffix -XX
                    // způsobuje selhání TryParse a vrácení Q260001 místo nového čísla.
                    var candidates = await db.Quotes
                        .Where(q => q.QuoteNumber.StartsWith("Q26") && !q.QuoteNumber.Contains("-"))
                        .Select(q => q.QuoteNumber)
                        .ToListAsync();

                    int nextNum = 1;
                    foreach (var qn in candidates)
                    {
                        if (qn.Length > 3 && int.TryParse(qn.Substring(3), out int num))
                            nextNum = Math.Max(nextNum, num + 1);
                    }

                    string newNumber = $"Q26{nextNum:D4}";
                    return Results.Json(new { quoteNumber = newNumber });
                }
                catch (Exception ex)
                {
                    // Podrobné logování do konzole
                    Console.WriteLine("=====================================");
                    Console.WriteLine("CHYBA v endpointu GET /next-quote");
                    Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Zpráva: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=====================================");

                    // Vrátíme klientovi čitelnou chybu (500)
                    return Results.Problem(
                        detail: "Došlo k chybě při generování dalšího čísla nabídky. Zkuste to později.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("GetNextQuote");

            // případně další podobné endpointy sem...

            return app;
        }
    }
}