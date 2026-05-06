using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RestAPI1.Endpoints;

public static class QuoteIncotermsEndpoint
{
    public static IEndpointRouteBuilder MapQuoteIncoterms(this IEndpointRouteBuilder app)
    {
        // GET /QuoteIncoterms — číselník Incoterms z Pohody (sVPULpol, RefAg = 2)
        app.MapGet("/QuoteIncoterms", async (IConfiguration config) =>
        {
            var cs = config.GetConnectionString("PohodaConnection")
                ?? throw new InvalidOperationException("PohodaConnection missing in appsettings");

            const string sql = """
                SELECT ID, IDS, SText
                FROM dbo.sVPULpol
                WHERE RefAg = 2
                ORDER BY OrderFld, IDS;
                """;

            try
            {
                await using var conn = new SqlConnection(cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                while (await reader.ReadAsync())
                {
                    var ids = reader["IDS"]?.ToString()?.Trim() ?? "";
                    var sText = reader["SText"]?.ToString()?.Trim() ?? "";
                    var value = string.IsNullOrEmpty(sText) ? ids : sText;
                    if (string.IsNullOrEmpty(value)) continue;

                    list.Add(new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("ID")),
                        ids,
                        text = sText,
                        value
                    });
                }

                return Results.Json(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QuoteIncoterms] ERROR: {ex.Message}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error loading incoterms from sVPULpol");
            }
        }).WithName("QuoteIncoterms");

        return app;
    }
}
