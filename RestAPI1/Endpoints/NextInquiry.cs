using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints
{
    public static class NextInquiryEndpoints
    {
        public static IEndpointRouteBuilder MapNextInquiry(this IEndpointRouteBuilder app)
        {
            // GET /next-inquiry – vrátí další volné číslo nabídky (I26xxxx)
            app.MapGet("/next-inquiry", async (BomDb db) =>
{
    try
    {
        Console.WriteLine("[next-inquiry] START – hledám poslední I26...");

        var lastNumber = await db.Inquiries
            .Where(q => q.InquiryNumber != null && q.InquiryNumber.StartsWith("I26"))
            .OrderByDescending(q => q.InquiryNumber)
            .Select(q => q.InquiryNumber)
            .FirstOrDefaultAsync();

        Console.WriteLine($"[next-inquiry] Poslední číslo: '{lastNumber ?? "žádné"}'");

        int nextNum = 1;
        if (!string.IsNullOrEmpty(lastNumber) && lastNumber.StartsWith("I26"))
        {
            string numPart = lastNumber.Length > 3 ? lastNumber.Substring(3) : "";
            if (int.TryParse(numPart, out int num) && num >= 0)
            {
                nextNum = num + 1;
            }
            else
            {
                Console.WriteLine($"[next-inquiry] Nelze parsovat číslo z '{lastNumber}' → startuji od 1");
            }
        }

        string newNumber = $"I26{nextNum:D4}";
        Console.WriteLine($"[next-inquiry] Generuji: {newNumber}");

        return Results.Json(new { inquiryNumber = newNumber });
    }
    catch (Exception ex)
    {
        Console.WriteLine("=====================================");
        Console.WriteLine("CHYBA v endpointu GET /next-inquiry");
        Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine($"Zpráva: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner: {ex.InnerException.Message}");
        }
        Console.WriteLine("=====================================");

        return Results.Problem(
            detail: "Došlo k chybě při generování dalšího čísla nabídky. Zkuste to později.",
            statusCode: 500,
            title: "Interní chyba serveru"
        );
    }
})
            .WithName("NextInquiry");

            // případně další podobné endpointy sem...

            return app;
        }
    }
}