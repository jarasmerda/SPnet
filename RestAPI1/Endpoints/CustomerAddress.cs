using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RestAPI1.Endpoints;

public static class CustomerAddressEndpoint
{
    public static IEndpointRouteBuilder MapCustomerAddress(this IEndpointRouteBuilder app)
    {
        app.MapGet("/CustomerAddress", async (HttpContext context, IConfiguration config) =>
        {
            var refADStr = context.Request.Query["refAD"].ToString().Trim();

            if (!int.TryParse(refADStr, out int refAD) || refAD <= 0)
                return Results.BadRequest(new { message = "Missing or invalid 'refAD' parameter (must be positive integer)" });

            var cs = config.GetConnectionString("PohodaConnection")
                ?? throw new InvalidOperationException("PohodaConnection missing in appsettings");

            const string sql = """
                SELECT
                    ad.ID,
                    ad.Cislo,
                    ad.Firma,
                    ad.Firma2,
                    ad.Jmeno,
                    ad.Ulice,
                    ad.PSC,
                    ad.Obec,
                    z1.IDS  AS ZemeKod,
                    z1.SText AS ZemeNazev,
                    ad.Ulice2,
                    ad.PSC2,
                    ad.Obec2,
                    z2.IDS  AS Zeme2Kod,
                    z2.SText AS Zeme2Nazev,
                    ad.ICO,
                    ad.DIC,
                    ad.Tel,
                    ad.GSM,
                    ad.Fax,
                    ad.Email,
                    ad.WWW
                FROM [dbo].[AD] ad
                LEFT JOIN [dbo].[sZeme] z1 ON ad.RefZeme  = z1.ID
                LEFT JOIN [dbo].[sZeme] z2 ON ad.RefZeme2 = z2.ID
                WHERE ad.ID = @refAD
                """;

            try
            {
                await using var conn = new SqlConnection(cs);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@refAD", refAD);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return Results.NotFound(new { message = $"Customer with refAD={refAD} not found in Pohoda AD" });

                string? Get(string col)
                {
                    var v = reader[col]?.ToString()?.Trim();
                    return string.IsNullOrEmpty(v) ? null : v;
                }

                var result = new
                {
                    id        = reader.GetInt32(reader.GetOrdinal("ID")),
                    cislo     = Get("Cislo"),

                    // Fakturační adresa
                    firma     = Get("Firma"),
                    firma2    = Get("Firma2"),
                    jmeno     = Get("Jmeno"),
                    ulice     = Get("Ulice"),
                    psc       = Get("PSC"),
                    obec      = Get("Obec"),
                    zemeKod   = Get("ZemeKod"),
                    zemeNazev = Get("ZemeNazev"),

                    // Dodací adresa
                    ulice2    = Get("Ulice2"),
                    psc2      = Get("PSC2"),
                    obec2     = Get("Obec2"),
                    zeme2Kod  = Get("Zeme2Kod"),
                    zeme2Nazev = Get("Zeme2Nazev"),

                    // Kontaktní údaje
                    ico   = Get("ICO"),
                    dic   = Get("DIC"),
                    tel   = Get("Tel"),
                    gsm   = Get("GSM"),
                    fax   = Get("Fax"),
                    email = Get("Email"),
                    www   = Get("WWW")
                };

                return Results.Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CustomerAddress] ERROR refAD={refAD}: {ex.Message}");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error querying Pohoda AD table"
                );
            }
        })
        .WithName("CustomerAddress");

        return app;
    }
}
