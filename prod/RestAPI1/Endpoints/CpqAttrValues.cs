using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;  // ← tvůj SmercoDb

namespace RestAPI1.Endpoints
{
    public static class Attr
    {
        public static IEndpointRouteBuilder MapCpqAttrValues(this IEndpointRouteBuilder app)
        {
            // GET /attr1 až /attr7 – vrací unikátní hodnoty atributu jako text/plain s novými řádky
            for (int i = 1; i <= 9; i++)
            {
                int attrNumber = i;
                app.MapGet($"/CpqAttrValues{attrNumber}", async (BomDb db) =>
                {
                    var values = await db.AttributeValues
                        .Where(r => r.AttributeNumber == attrNumber 
                                 && !string.IsNullOrWhiteSpace(r.AttributeValue))
                        .Select(r => r.AttributeValue!.Trim())
                        .Distinct()
                        .OrderBy(v => v)
                        .ToListAsync();

                    return Results.Text(string.Join("\n", values), "text/plain; charset=utf-8");
                })
                .WithName($"Attr{attrNumber}");
            }

            return app;
        }
    }
}