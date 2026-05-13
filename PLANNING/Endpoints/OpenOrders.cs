using Microsoft.Data.SqlClient;
using Planning.Models;

namespace Planning.Endpoints;

public static class OpenOrdersEndpoints
{
    public static IEndpointRouteBuilder MapOpenOrders(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/open-orders", async (IConfiguration config) =>
        {
            var connStr = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection není nakonfigurován.");

            var orders = new List<OpenOrderDto>();

            try
            {
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand("SELECT * FROM dbo.vw_OpenOrders ORDER BY Date DESC", conn)
                {
                    CommandTimeout = 60
                };
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    orders.Add(new OpenOrderDto(
                        DocNumber:           reader.GetStringSafe("DocNumber"),
                        Company:             reader.GetStringSafe("Company"),
                        Code:                reader.GetStringSafe("Code"),
                        Description:         reader.GetStringSafe("Description"),
                        Note:                reader.GetStringSafe("Note"),
                        Batch:               reader.GetStringSafe("Batch"),
                        Qty:                 reader.GetDecimalSafe("Qty"),
                        Uom:                 reader.GetStringSafe("Uom"),
                        Delivered:           reader.GetDecimalSafe("Delivered"),
                        OpenQty:             reader.GetDecimalSafe("OpenQty"),
                        DocType:             reader.GetStringSafe("DocType"),
                        CodeType:            reader.GetStringSafe("CodeType"),
                        TraRef:              reader.GetStringSafe("TraRef"),
                        OrderNumber:         reader.GetStringSafe("OrderNumber"),
                        DeliveryWeek:        reader.GetStringSafe("DeliveryWeek"),
                        WastePercent:        reader.GetDecimalNullable("WastePercent"),
                        YesVsNo:             reader.GetStringSafe("YesVsNo"),
                        MonoBi:              reader.GetStringSafe("MonoBi"),
                        Color:               reader.GetStringSafe("Color"),
                        Country:             reader.GetStringSafe("Country"),
                        Kg:                  reader.GetDecimalSafe("Kg"),
                        OpenKg:              reader.GetDecimalSafe("OpenKg"),
                        ValueEurCalc:        reader.GetDecimalSafe("ValueEurCalc"),
                        PriceLoc:            reader.GetDecimalSafe("PriceLoc"),
                        ValueLoc:            reader.GetDecimalSafe("ValueLoc"),
                        PriceCur:            reader.GetDecimalNullable("PriceCur"),
                        ValueCur:            reader.GetDecimalNullable("ValueCur"),
                        Incoterms:           reader.GetStringSafe("Incoterms"),
                        Date:                reader.GetDateTimeNullable("Date"),
                        DeliveryDate:        reader.GetDateTimeNullable("DeliveryDate"),
                        TruckDate:           reader.GetDateTimeNullable("TruckDate"),
                        Availability:        reader.GetDateTimeNullable("Availability"),
                        CustomerOrderRef:    reader.GetStringSafe("CustomerOrderRef"),
                        CustomerOrderRefNew: reader.GetStringSafe("CustomerOrderRefNew"),
                        DateOfOrder:         reader.GetStringSafe("DateOfOrder"),
                        ItemAccount:         reader.GetStringSafe("ItemAccount"),
                        IsOpen:              reader.GetBoolSafe("IsOpen")
                    ));
                }

                return Results.Ok(orders);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null
                    ? $"{ex.Message} | Inner: {ex.InnerException.Message}"
                    : ex.Message;
                Console.WriteLine($"[OPEN-ORDERS ERROR] {detail}\n{ex.StackTrace}");
                return Results.Problem(detail: detail, statusCode: 500,
                    title: "Chyba při načítání otevřených objednávek");
            }
        })
        .WithName("GetOpenOrders")
        .WithTags("Planning");

        return app;
    }
}

internal static class SqlReaderExtensions
{
    public static string GetStringSafe(this SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return "";
        return r.GetValue(ord) switch
        {
            string s    => s,
            bool b      => b ? "1" : "0",
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            var v       => v.ToString() ?? ""
        };
    }

    public static decimal GetDecimalSafe(this SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return 0m;
        return r.GetFieldValue<object>(ord) switch
        {
            decimal d  => d,
            double dbl => (decimal)dbl,
            float f    => (decimal)f,
            int i      => i,
            long l     => l,
            _          => 0m
        };
    }

    public static decimal? GetDecimalNullable(this SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return null;
        return r.GetFieldValue<object>(ord) switch
        {
            decimal d  => d,
            double dbl => (decimal)dbl,
            float f    => (decimal)f,
            int i      => i,
            _          => null
        };
    }

    public static DateTime? GetDateTimeNullable(this SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        return r.IsDBNull(ord) ? null : r.GetDateTime(ord);
    }

    public static bool GetBoolSafe(this SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return false;
        return r.GetFieldValue<object>(ord) switch
        {
            bool b   => b,
            int i    => i != 0,
            byte b2  => b2 != 0,
            _        => false
        };
    }
}
