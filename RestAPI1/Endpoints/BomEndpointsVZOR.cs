using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints
{
    public static class QuotesEndpoints
    {
        public static IEndpointRouteBuilder MapBomEndpointsVZOR(this IEndpointRouteBuilder app)
        {
            // GET /quotes – seznam všech nabídek
            app.MapGet("/BomEndpointsVZOR", async (BomDb db) =>
            {
                var quotes = await db.Quotes
                    .OrderByDescending(q => q.LastSaved)
                    .Select(q => new
                    {
                        q.QuoteNumber,
                        q.Status,
                        q.CustomerName,
                        created = q.CreatedDate.ToString("dd.MM.yyyy HH:mm"),
                        lastSaved = q.LastSaved.ToString("dd.MM.yyyy HH:mm")
                    })
                    .ToListAsync();

                return Results.Json(quotes);
            })
            .WithName("GetAllQuotesVZOR");

            // případně další endpointy pro "quotes" oblast sem...

            return app;
        }
    }
}