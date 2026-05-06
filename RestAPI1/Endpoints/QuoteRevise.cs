using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints;

public static class QuoteReviseEndpoint
{
    public static IEndpointRouteBuilder MapQuoteRevise(this IEndpointRouteBuilder app)
    {
        // POST /quote/{number}/revise
        // Zkopíruje schválenou nabídku jako novou revizi (např. Q260063 → Q260063-01)
        // Nová revize má status IN PROGRESS a je plně editovatelná.
        app.MapPost("/quote/{number}/revise", async (BomDb db, string number) =>
        {
            try
            {
                // 1. Najdi originální quote
                var original = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteNumber == number);
                if (original == null)
                    return Results.NotFound(new { message = $"Quote '{number}' not found" });

                // 2. Urči base číslo (odstraň případnou příponu -01, -02, ...)
                var baseNumber = System.Text.RegularExpressions.Regex.Replace(number, @"-\d{2}$", "");

                // 3. Najdi všechny existující revize tohoto base čísla
                var prefix = baseNumber + "-";
                var existingRevisions = await db.Quotes
                    .Where(q => q.QuoteNumber == baseNumber || q.QuoteNumber.StartsWith(prefix))
                    .Select(q => q.QuoteNumber)
                    .ToListAsync();

                // 4. Urči příští číslo revize
                int nextRev = 1;
                foreach (var qNum in existingRevisions)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(qNum, @"-(\d{2})$");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int rev))
                        nextRev = Math.Max(nextRev, rev + 1);
                }

                var newNumber = $"{baseNumber}-{nextRev:D2}";

                // 5. Zkontroluj, že nové číslo ještě neexistuje
                if (await db.Quotes.AnyAsync(q => q.QuoteNumber == newNumber))
                    return Results.Conflict(new { message = $"Revision '{newNumber}' already exists" });

                // 6. Zkopíruj header – nový status = IN PROGRESS
                var revision = new QuoteHeader
                {
                    QuoteNumber   = newNumber,
                    Status        = "IN PROGRESS",
                    CustomerID    = original.CustomerID,
                    CustomerName  = original.CustomerName,
                    CreatedDate   = DateTime.Now,
                    LastSaved     = DateTime.Now,
                    QuoteDate     = DateTime.Today,
                    ValidUntil    = original.ValidUntil,
                    RequestDate   = original.RequestDate,
                    Incoterms     = original.Incoterms,
                    DeliveryWeeks = original.DeliveryWeeks,
                    PdfColor      = original.PdfColor,
                };

                db.Quotes.Add(revision);
                await db.SaveChangesAsync(); // → získáme revision.QuoteID

                // 7. Zkopíruj všechny položky
                var originalItems = await db.QuoteItems
                    .Where(i => i.QuoteID == original.QuoteID)
                    .ToListAsync();

                foreach (var item in originalItems)
                {
                    db.QuoteItems.Add(new QuoteItem
                    {
                        QuoteID      = revision.QuoteID,
                        Code         = item.Code,
                        Name         = item.Name,
                        Attr1        = item.Attr1,
                        Attr2        = item.Attr2,
                        Attr3        = item.Attr3,
                        Attr4        = item.Attr4,
                        Attr5        = item.Attr5,
                        Attr6        = item.Attr6,
                        Attr7        = item.Attr7,
                        Quantity     = item.Quantity,
                        CostPrice    = item.CostPrice,
                        SellingPrice = item.SellingPrice,
                    });
                }

                await db.SaveChangesAsync();

                Console.WriteLine($"[QuoteRevise] Vytvořena revize {newNumber} z {number}");

                return Results.Ok(new { newQuoteNumber = newNumber });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QuoteRevise] ERROR: {ex.Message}\n{ex.StackTrace}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error creating quote revision"
                );
            }
        })
        .WithName("QuoteRevise");

        return app;
    }
}
