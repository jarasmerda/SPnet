using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace RestAPI1.Endpoints
{
    public static class QuoteNumber
    {
        // Pomocná statická metoda – musí být před použitím v lambda výrazech
        private static string? GetNestedString(JsonElement element, string objName, string propName)
        {
            if (element.TryGetProperty(objName, out var obj) &&
                obj.ValueKind == JsonValueKind.Object &&
                obj.TryGetProperty(propName, out var prop))
            {
                return prop.GetString();
            }
            return null;
        }

        // ✅ NOVÉ: bezpečné čtení int z JSON
        private static int GetInt(JsonElement element, string propName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propName, out var p) && p.ValueKind == JsonValueKind.Number)
                return p.GetInt32();

            // někdy může přijít itemId jako string "123"
            if (element.TryGetProperty(propName, out var ps) && ps.ValueKind == JsonValueKind.String &&
                int.TryParse(ps.GetString(), out var parsed))
                return parsed;

            return defaultValue;
        }

        public static IEndpointRouteBuilder MapQuoteNumber(this IEndpointRouteBuilder app)
        {
            // GET /quote/{number} – detail nabídky (vytvoří novou, pokud neexistuje)
            app.MapGet("/quote/{number}", async (BomDb db, string number) =>
            {
                try
                {
                    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteNumber == number);

                    if (quote == null)
                    {
                        Console.WriteLine($"Vytvářím novou nabídku: {number}");
                        quote = new QuoteHeader
                        {
                            QuoteNumber = number,
                            Status = "WORKING ON IT",
                            CreatedDate = DateTime.Now,
                            LastSaved = DateTime.Now,
                            QuoteDate = DateTime.Today,
                            ValidUntil = DateTime.Today.AddMonths(1),
                            CustomerID = null,
                            CustomerName = null
                        };
                        db.Quotes.Add(quote);
                        await db.SaveChangesAsync();
                    }

                    var items = await db.QuoteItems
                        .Where(i => i.QuoteID == quote.QuoteID)
                        .Select(i => new
                        {
                            i.Code,
                            i.Name,
                            itemId = i.ItemID,   // ✅ ItemID jde ven do frontendu
                            attributes = new
                            {
                                attr1 = i.Attr1 ?? "",
                                attr2 = i.Attr2 ?? "",
                                attr3 = i.Attr3 ?? "",
                                attr4 = i.Attr4 ?? "",
                                attr5 = i.Attr5 ?? "",
                                attr6 = i.Attr6 ?? "",
                                attr7 = i.Attr7 ?? ""
                            },
                            i.Quantity,
                            i.CostPrice,
                            i.SellingPrice
                        })
                        .ToListAsync();

                    return Results.Json(new
                    {
                        quoteNumber = quote.QuoteNumber,
                        status = quote.Status,
                        customerId = quote.CustomerID ?? "",
                        customerName = quote.CustomerName ?? "",
                        date = quote.QuoteDate?.ToString("yyyy-MM-dd") ?? "",
                        validUntil = quote.ValidUntil?.ToString("yyyy-MM-dd") ?? "",
                        items
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=====================================");
                    Console.WriteLine($"CHYBA v endpointu GET /quote/{{{number}}}");
                    Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Zpráva: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=====================================");

                    return Results.Problem(
                        detail: "Došlo k chybě při načítání nebo vytváření nabídky. Zkuste to později.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("QuoteNumber");

            // POST /quote/{number} – uložení nabídky + odeslání emailu při změně na "WAITING FOR APPROVAL"
            app.MapPost("/quote/{number}", async (BomDb db, string number, HttpContext context) =>
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] POST /quote/{number} voláno");
                Console.WriteLine($"Remote IP:          {context.Connection.RemoteIpAddress?.ToString() ?? "neznámá"}");
                Console.WriteLine($"User-Agent:         {context.Request.Headers["User-Agent"].ToString() ?? "není"}");
                Console.WriteLine($"Referer:            {context.Request.Headers["Referer"].ToString() ?? "není"}");
                Console.WriteLine($"Origin:             {context.Request.Headers["Origin"].ToString() ?? "není"}");
                Console.WriteLine($"Authorization:      {(context.Request.Headers["Authorization"].ToString().StartsWith("Bearer") ? "Bearer token (skrýváno)" : context.Request.Headers["Authorization"].ToString() ?? "není")}");
                Console.WriteLine("Body preview (prvních 500 znaků):");

                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine(body.Length > 500 ? body.Substring(0, 500) + "..." : body);
                context.Request.Body.Position = 0;

                Console.WriteLine("═══════════════════════════════════════════════════════════════");

                JsonElement jsonElement;
                try
                {
                    jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(new MemoryStream(Encoding.UTF8.GetBytes(body)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JSON PARSE ERROR] {ex.Message}");
                    return Results.BadRequest(new { error = "Neplatný JSON formát" });
                }

                var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteNumber == number);

                string originalStatus = quote?.Status ?? "WORKING ON IT";
                bool isNewQuote = quote == null;

                if (isNewQuote)
                {
                    quote = new QuoteHeader
                    {
                        QuoteNumber = number,
                        Status = "WORKING ON IT",
                        CreatedDate = DateTime.Now,
                        LastSaved = DateTime.Now,
                        QuoteDate = DateTime.Today,
                        ValidUntil = DateTime.Today.AddMonths(1),
                        CustomerID = null,
                        CustomerName = null
                    };
                    db.Quotes.Add(quote);
                }

                // Aktualizace headeru z JSONu
                if (jsonElement.TryGetProperty("status", out var statusProp))
                {
                    quote.Status = statusProp.GetString() ?? quote.Status;
                }

                if (jsonElement.TryGetProperty("customerId", out var cid) && cid.ValueKind != JsonValueKind.Null)
                    quote.CustomerID = cid.GetString();

                if (jsonElement.TryGetProperty("customerName", out var cname) && cname.ValueKind != JsonValueKind.Null)
                    quote.CustomerName = cname.GetString();

                if (jsonElement.TryGetProperty("date", out var dateProp) &&
                    dateProp.ValueKind != JsonValueKind.Null &&
                    dateProp.GetString() is string dateStr &&
                    DateTime.TryParse(dateStr, out var parsedDate))
                {
                    quote.QuoteDate = parsedDate;
                }

                if (jsonElement.TryGetProperty("validUntil", out var validProp) &&
                    validProp.ValueKind != JsonValueKind.Null &&
                    validProp.GetString() is string validStr &&
                    DateTime.TryParse(validStr, out var parsedValid))
                {
                    quote.ValidUntil = parsedValid;
                }

                quote.LastSaved = DateTime.Now;

                await db.SaveChangesAsync();  // Uložíme header → máme QuoteID

                // ✅ UPSERT položek – NE smaž+vlož (ItemID zůstane stabilní)
                var existingItems = await db.QuoteItems
                    .Where(i => i.QuoteID == quote.QuoteID)
                    .ToListAsync();

                var keepIds = new HashSet<int>();

                if (jsonElement.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var itemEl in itemsElement.EnumerateArray())
                    {
                        var incomingItemId = GetInt(itemEl, "itemId", 0);
                        if (incomingItemId > 0)
                            keepIds.Add(incomingItemId);

                        QuoteItem dbItem;

                        if (incomingItemId > 0)
                        {
                            dbItem = existingItems.FirstOrDefault(x => x.ItemID == incomingItemId);

                            // pokud přišlo itemId, ale v DB není (např. race condition), vložíme jako nový řádek
                            if (dbItem == null)
                            {
                                dbItem = new QuoteItem { QuoteID = quote.QuoteID };
                                db.QuoteItems.Add(dbItem);
                            }
                        }
                        else
                        {
                            // nový řádek
                            dbItem = new QuoteItem { QuoteID = quote.QuoteID };
                            db.QuoteItems.Add(dbItem);
                        }

                        dbItem.Code = itemEl.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";
                        dbItem.Name = itemEl.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

                        dbItem.Attr1 = GetNestedString(itemEl, "attributes", "attr1");
                        dbItem.Attr2 = GetNestedString(itemEl, "attributes", "attr2");
                        dbItem.Attr3 = GetNestedString(itemEl, "attributes", "attr3");
                        dbItem.Attr4 = GetNestedString(itemEl, "attributes", "attr4");
                        dbItem.Attr5 = GetNestedString(itemEl, "attributes", "attr5");
                        dbItem.Attr6 = GetNestedString(itemEl, "attributes", "attr6");
                        dbItem.Attr7 = GetNestedString(itemEl, "attributes", "attr7");

                        dbItem.Quantity = itemEl.TryGetProperty("quantity", out var q) && q.ValueKind == JsonValueKind.Number ? q.GetInt32() : 1;
                        dbItem.CostPrice = itemEl.TryGetProperty("costPrice", out var cp) && cp.ValueKind == JsonValueKind.Number ? cp.GetDecimal() : 0m;
                        dbItem.SellingPrice = itemEl.TryGetProperty("sellingPrice", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetDecimal() : 0m;
                    }
                }

                // DELETE: z DB smažeme jen ty řádky, které už frontend neposílá
                var toDelete = existingItems.Where(x => !keepIds.Contains(x.ItemID)).ToList();
                db.QuoteItems.RemoveRange(toDelete);

                await db.SaveChangesAsync();

                // ODESLÁNÍ EMAILU – jen při přechodu na "WAITING FOR APPROVAL"
                bool shouldSendApprovalEmail =
                    quote.Status == "WAITING FOR APPROVAL" &&
                    originalStatus != "WAITING FOR APPROVAL";

                if (shouldSendApprovalEmail)
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer",
                            "re_iGN7eBqf_tqgqhYeiNrEKHZNN8q2pBfUA"   // DOPORUČENÍ: přesuň do konfigurace!
                        );

                        var payload = new
                        {
                            from = "HDPE CRM <info@smerco.cz>",
                            to = new[] { "info@smerco.cz" },           // uprav na skutečné schvalovatele
                            subject = $"Nová nabídka WAITING FOR APPROVAL: {quote.QuoteNumber}",
                            html = $@"
                                <h2>Nová cenová nabídka WAITING FOR APPROVAL</h2>
                                <p><strong>Číslo nabídky:</strong> {quote.QuoteNumber}</p>
                                <p><strong>Zákazník:</strong> {(quote.CustomerName ?? "neuveden")}</p>
                                <p><strong>Datum nabídky:</strong> {(quote.QuoteDate?.ToString("dd.MM.yyyy") ?? "neuvedeno")}</p>
                                <p><strong>Platnost do:</strong> {(quote.ValidUntil?.ToString("dd.MM.yyyy") ?? "neuvedeno")}</p>
                                <br>
                                <p>
                                    <a href='http://185.219.164.45:5127/quote.html?quote={quote.QuoteNumber}' 
                                       style='padding:12px 24px; background:#007bff; color:white; text-decoration:none; border-radius:6px; font-weight:bold;'>
                                        Otevřít nabídku ke schválení
                                    </a>
                                </p>
                                <p style='margin-top:24px; font-size:0.9em; color:#666;'>
                                    Tato zpráva byla vygenerována automaticky systémem HDPE CRM.<br>
                                    Prosím nereagujte přímo na tento email.
                                </p>"
                        };

                        var json = JsonSerializer.Serialize(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("https://api.resend.com/emails", content);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[EMAIL SUCCESS] Odeslán email pro nabídku {quote.QuoteNumber}");
                        }
                        else
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"[EMAIL FAILED] {response.StatusCode} – {errorBody}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EMAIL EXCEPTION] {ex.Message}\n{ex.StackTrace}");
                        // neblokujeme uložení
                    }
                }

                return Results.Ok();
            })
            .WithName("SaveQuoteWithApprovalEmail");

            return app;
        }
    }
}
