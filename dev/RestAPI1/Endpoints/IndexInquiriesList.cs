using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints
{
    public static class Inquiries
    {
        public static IEndpointRouteBuilder MapIndexInquiriesList(this IEndpointRouteBuilder app)
        {
            // GET /inquiries – seznam všech nabídek
            app.MapGet("IndexInquiriesList", async (BomDb db) =>
            {
                try
                {
                    var inquiries = await db.Inquiries
                        .OrderByDescending(q => q.LastSaved)
                        .Select(q => new
                        {
                            q.InquiryNumber,
                            q.Status,
                            q.CustomerName,
                            created = q.CreatedDate.ToString("dd.MM.yyyy HH:mm"),
                            lastSaved = q.LastSaved.ToString("dd.MM.yyyy HH:mm")
                        })
                        .ToListAsync();

                    return Results.Json(inquiries);
                }
                catch (Exception ex)
                {
                    // Logování do konzole (vidíš přesně, co se stalo)
                    Console.WriteLine("=====================================");
                    Console.WriteLine("CHYBA v endpointu GET /inquiries");
                    Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Zpráva: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=====================================");

                    // Vrátíme smysluplnou chybu klientovi
                    return Results.Problem(
                        detail: "Došlo k chybě při načítání seznamu nabídek. Zkuste to později.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("IndexInquiriesList");

            // případně další endpointy pro "inquiries" oblast sem...

            return app;
        }
    }
}