using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints
{
    public static class LookupItemEndpoints
    {
        public static IEndpointRouteBuilder MapQuotePohodaItemDetail(this IEndpointRouteBuilder app)
        {
            app.MapGet("QuotePohodaItemDetail", async (BomDb db, string code) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        return Results.BadRequest(new { found = false, message = "Chybí kód" });
                    }

                    code = code.Trim();

                    // Načteme položku z SKz včetně atributů (VPrAttr1 až VPrAttr7)
                    var skzItem = await db.SKz
                        .Where(p => p.IDS == code)
                        .Select(p => new
                        {
                            Code = p.IDS,
                            Name = p.DESCRIPTION ?? "Bez názvu",
                            Attr1 = p.VPrAttr1 ?? "",   // ← přidáno: načítání atributů
                            Attr2 = p.VPrAttr2 ?? "",
                            Attr3 = p.VPrAttr3 ?? "",
                            Attr4 = p.VPrAttr4 ?? "",
                            Attr5 = p.VPrAttr5 ?? "",
                            Attr6 = p.VPrAttr6 ?? "",
                            Attr7 = p.VPrAttr7 ?? "",
                            CostPrice = p.PURCHASE_PRICE,
                            DefaultQuantity = 1
                        })
                        .FirstOrDefaultAsync();

                    if (skzItem != null)
                    {
                        return Results.Ok(new
                        {
                            found = true,
                            source = "SKz",
                            code = skzItem.Code,
                            name = skzItem.Name,
                            attr1 = skzItem.Attr1,   // ← teď vrací reálné hodnoty
                            attr2 = skzItem.Attr2,
                            attr3 = skzItem.Attr3,
                            attr4 = skzItem.Attr4,
                            attr5 = skzItem.Attr5,
                            attr6 = skzItem.Attr6,
                            attr7 = skzItem.Attr7,
                            defaultQuantity = skzItem.DefaultQuantity,
                            costPrice = skzItem.CostPrice
                        });
                    }

                    // Kód nenalezen
                    return Results.Ok(new { found = false, message = "Kód nenalezen v SKz" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=====================================");
                    Console.WriteLine($"CHYBA v endpointu GET /QuotePohodaItemDetail (code = {code})");
                    Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Zpráva: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=====================================");

                    return Results.Problem(
                        detail: "Došlo k chybě při vyhledávání položky v SKz. Zkuste to později.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("QuotePohodaItemDetail");

            return app;
        }
    }
}