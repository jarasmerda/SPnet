using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Endpoints
{
    public static class PohodaFindIdsByAttrs
    {
        public record FindIdsRequest(int ItemId, string? Attr8, string? Attr9);

        private static string Norm(string? s) => (s ?? "").Trim();

        public static IEndpointRouteBuilder MapOrdersFindIdsByAttrsPohoda(this IEndpointRouteBuilder app)
        {
            // POST /pohoda/find-ids-by-attrs
            // Body: { "itemId": 333, "attr8": "BLUE", "attr9": "A1" }
            // itemId slouží POUZE k načtení Attr1..7 z tab.QuoteItems.
            // V dbo.SKz hledáme kombinaci Attr1..Attr9 (itemId se v SKz nikdy nepoužije).
            app.MapPost("/OrdersFindIdsByAttrsPohoda", async (BomDb db, FindIdsRequest req) =>
            {
                // 1) Načti Attr1..7 z QuoteItems podle ItemId
                var qi = await db.QuoteItems
                    .AsNoTracking()
                    .Where(x => x.ItemID == req.ItemId)
                    .Select(x => new
                    {
                        x.Attr1, x.Attr2, x.Attr3, x.Attr4, x.Attr5, x.Attr6, x.Attr7
                    })
                    .FirstOrDefaultAsync();

                if (qi is null)
                {
                    return Results.NotFound(new
                    {
                        ids = (string?)null,
                        message = $"QuoteItem ItemID={req.ItemId} not found."
                    });
                }

                // 2) Normalizuj hodnoty (trim) mimo SQL
                var a1 = Norm(qi.Attr1);
                var a2 = Norm(qi.Attr2);
                var a3 = Norm(qi.Attr3);
                var a4 = Norm(qi.Attr4);
                var a5 = Norm(qi.Attr5);
                var a6 = Norm(qi.Attr6);
                var a7 = Norm(qi.Attr7);
                var a8 = Norm(req.Attr8);
                var a9 = Norm(req.Attr9);

                // 3) Hledej v dbo.SKz kombinaci Attr1..Attr9 a vrať IDS
                var ids = await db.SKz
                    .AsNoTracking()
                    .Where(s =>
                        (s.VPrAttr1 ?? "").Trim() == a1 &&
                        (s.VPrAttr2 ?? "").Trim() == a2 &&
                        (s.VPrAttr3 ?? "").Trim() == a3 &&
                        (s.VPrAttr4 ?? "").Trim() == a4 &&
                        (s.VPrAttr5 ?? "").Trim() == a5 &&
                        (s.VPrAttr6 ?? "").Trim() == a6 &&
                        (s.VPrAttr7 ?? "").Trim() == a7 &&
                        (s.VPrAttr8 ?? "").Trim() == a8 &&
                        (s.VPrAttr9 ?? "").Trim() == a9
                    )
                    .Select(s => s.IDS)
                    .FirstOrDefaultAsync();

                return Results.Json(new { ids });
            })
            .WithName("OrdersFindIdsByAttrsPohoda");

            return app;
        }
    }
}
