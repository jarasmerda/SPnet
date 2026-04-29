using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace RestAPI1.Endpoints
{
    public static class InquiryNumber
    {
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

        private static int GetInt(JsonElement element, string propName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propName, out var p) && p.ValueKind == JsonValueKind.Number)
                return p.GetInt32();

            if (element.TryGetProperty(propName, out var ps) && ps.ValueKind == JsonValueKind.String &&
                int.TryParse(ps.GetString(), out var parsed))
                return parsed;

            return defaultValue;
        }

        /// <summary>
        /// Najde nebo vytvoří záznam InquiryHeader. Uloží do DB ihned (používá se hlavně v GET).
        /// </summary>
        private static async Task<InquiryHeader> GetOrCreateInquiryAsync(BomDb db, string number)
        {
            var inquiry = await db.Inquiries
                .FirstOrDefaultAsync(q => q.InquiryNumber == number);

            if (inquiry != null)
                return inquiry;

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Vytvářím novou nabídku: {number}");

            inquiry = new InquiryHeader
            {
                InquiryNumber = number,
                Status = "WORKING ON IT",
                CreatedDate = DateTime.Now,
                LastSaved = DateTime.Now,
                InquiryDate = DateTime.Today,
                ValidUntil = DateTime.Today.AddMonths(1),
                CustomerID = null,
                CustomerName = null
            };

            db.Inquiries.Add(inquiry);
            await db.SaveChangesAsync();

            return inquiry;
        }

        public static IEndpointRouteBuilder MapInquiryNumber(this IEndpointRouteBuilder app)
        {
            // GET /inquiry/{number} – vrátí detail (vytvoří novou, pokud neexistuje)
            app.MapGet("/inquiry/{number}", async (BomDb db, string number) =>
            {
                try
                {
                    var inquiry = await GetOrCreateInquiryAsync(db, number);

                    var items = await db.InquiryItems
                        .Where(i => i.InquiryID == inquiry.InquiryID)
                        .Select(i => new
                        {
                            i.Code,
                            i.Name,
                            itemId = i.ItemID,
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
                        inquiryNumber = inquiry.InquiryNumber,
                        status = inquiry.Status,
                        customerId = inquiry.CustomerID ?? "",
                        customerName = inquiry.CustomerName ?? "",
                        date = inquiry.InquiryDate?.ToString("yyyy-MM-dd") ?? "",
                        validUntil = inquiry.ValidUntil?.ToString("yyyy-MM-dd") ?? "",
                        items
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine($"CHYBA v GET /inquiry/{number}  {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    if (ex.InnerException != null)
                        Console.WriteLine($"Inner: {ex.InnerException.Message}");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");

                    return Results.Problem(
                        detail: "Chyba při načítání / vytváření nabídky.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("GetInquiryByNumber");

            // POST /inquiry/{number} – pouze aktualizace existující nabídky
            app.MapPost("/inquiry/{number}", async (BomDb db, string number, HttpContext context, IConfiguration config, IHttpClientFactory httpClientFactory) =>
            {
                // Logování requestu (pro ladění / audit)
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine($"POST /inquiry/{number}   {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"IP: {context.Connection.RemoteIpAddress}");
                Console.WriteLine($"UA: {context.Request.Headers["User-Agent"]}");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");

                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body))
                    return Results.BadRequest(new { error = "Prázdné tělo požadavku" });

                JsonElement json;
                try
                {
                    json = JsonSerializer.Deserialize<JsonElement>(body);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[JSON ERROR] {ex.Message}");
                    return Results.BadRequest(new { error = "Neplatný JSON" });
                }

                var inquiry = await db.Inquiries
                    .FirstOrDefaultAsync(q => q.InquiryNumber == number);

                if (inquiry == null)
                {
                    return Results.NotFound(new 
                    { 
                        error = $"Nabídka {number} neexistuje. Nejprve zavolejte GET endpoint." 
                    });
                }

                string originalStatus = inquiry.Status;

                // Aktualizace headeru
                if (json.TryGetProperty("status", out var statusProp))
                    inquiry.Status = statusProp.GetString() ?? inquiry.Status;

                if (json.TryGetProperty("customerId", out var cid) && cid.ValueKind != JsonValueKind.Null)
                    inquiry.CustomerID = cid.GetString();

                if (json.TryGetProperty("customerName", out var cname) && cname.ValueKind != JsonValueKind.Null)
                    inquiry.CustomerName = cname.GetString();

                if (json.TryGetProperty("date", out var dateProp) &&
                    dateProp.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(dateProp.GetString(), out var dt))
                {
                    inquiry.InquiryDate = dt;
                }

                if (json.TryGetProperty("validUntil", out var validProp) &&
                    validProp.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(validProp.GetString(), out var validDt))
                {
                    inquiry.ValidUntil = validDt;
                }

                inquiry.LastSaved = DateTime.Now;

                await db.SaveChangesAsync();  // uložíme header

                // ────────────────────────────────────────────────
                // UPSERT položek
                var existingItems = await db.InquiryItems
                    .Where(i => i.InquiryID == inquiry.InquiryID)
                    .ToListAsync();

                var keepIds = new HashSet<int>();

                if (json.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var itemEl in itemsEl.EnumerateArray())
                    {
                        var itemId = GetInt(itemEl, "itemId", 0);
                        if (itemId > 0) keepIds.Add(itemId);

                        InquiryItem dbItem;

                        if (itemId > 0)
                        {
                            dbItem = existingItems.FirstOrDefault(x => x.ItemID == itemId);
                            if (dbItem == null)
                            {
                                // race condition ochrana
                                dbItem = new InquiryItem { InquiryID = inquiry.InquiryID };
                                db.InquiryItems.Add(dbItem);
                            }
                        }
                        else
                        {
                            dbItem = new InquiryItem { InquiryID = inquiry.InquiryID };
                            db.InquiryItems.Add(dbItem);
                        }

                        dbItem.Code        = itemEl.TryGetProperty("code",        out var c)   ? c.GetString()   ?? "" : "";
                        dbItem.Name        = itemEl.TryGetProperty("name",        out var n)   ? n.GetString()   ?? "" : "";
                        dbItem.Attr1       = GetNestedString(itemEl, "attributes", "attr1");
                        dbItem.Attr2       = GetNestedString(itemEl, "attributes", "attr2");
                        dbItem.Attr3       = GetNestedString(itemEl, "attributes", "attr3");
                        dbItem.Attr4       = GetNestedString(itemEl, "attributes", "attr4");
                        dbItem.Attr5       = GetNestedString(itemEl, "attributes", "attr5");
                        dbItem.Attr6       = GetNestedString(itemEl, "attributes", "attr6");
                        dbItem.Attr7       = GetNestedString(itemEl, "attributes", "attr7");

                        dbItem.Quantity    = itemEl.TryGetProperty("quantity",    out var q)   && q.ValueKind == JsonValueKind.Number ? q.GetInt32()   : 1;
                        dbItem.CostPrice   = itemEl.TryGetProperty("costPrice",   out var cp)  && cp.ValueKind == JsonValueKind.Number ? cp.GetDecimal() : 0m;
                        dbItem.SellingPrice = itemEl.TryGetProperty("sellingPrice", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetDecimal() : 0m;
                    }
                }

                // Smazání položek, které už nejsou v JSONu
                var toDelete = existingItems.Where(x => !keepIds.Contains(x.ItemID)).ToList();
                if (toDelete.Count > 0)
                {
                    db.InquiryItems.RemoveRange(toDelete);
                }

                await db.SaveChangesAsync();

                // ────────────────────────────────────────────────
                // Email při změně stavu na WAITING FOR APPROVAL
                if (inquiry.Status == "WAITING FOR APPROVAL" && originalStatus != "WAITING FOR APPROVAL")
                {
                    try
                    {
                        var resendApiKey = config["Resend:ApiKey"] ?? "";
                        var crmBaseUrl = config["App:CrmBaseUrl"] ?? "http://localhost:5127";
                        var fromAddress = config["Resend:FromAddress"] ?? "HDPE CRM <info@smerco.cz>";

                        using var client = httpClientFactory.CreateClient();
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resendApiKey);

                        var payload = new
                        {
                            from = fromAddress,
                            to = new[] { "info@smerco.cz" },
                            subject = $"Nová nabídka ke schválení: {inquiry.InquiryNumber}",
                            html = $"""
                                <h2>Nová cenová nabídka čeká na schválení</h2>
                                <p><strong>Číslo nabídky:</strong> {inquiry.InquiryNumber}</p>
                                <p><strong>Zákazník:</strong> {(inquiry.CustomerName ?? "neuveden")}</p>
                                <p><strong>Datum:</strong> {(inquiry.InquiryDate?.ToString("dd.MM.yyyy") ?? "neuvedeno")}</p>
                                <p><strong>Platnost do:</strong> {(inquiry.ValidUntil?.ToString("dd.MM.yyyy") ?? "neuvedeno")}</p>
                                <br>
                                <a href='{crmBaseUrl}/inquiry.html?inquiry={inquiry.InquiryNumber}'
                                   style='display:inline-block; padding:12px 24px; background:#007bff; color:white; text-decoration:none; border-radius:6px; font-weight:bold;'>
                                    Otevřít ke schválení
                                </a>
                                <p style='margin-top:24px; font-size:0.9em; color:#666;'>
                                    Automatická zpráva systému HDPE CRM – nereagujte prosím přímo na tento e-mail.
                                </p>
                                """
                        };

                        var jsonPayload = JsonSerializer.Serialize(payload);
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("https://api.resend.com/emails", content);

                        if (response.IsSuccessStatusCode)
                            Console.WriteLine($"[EMAIL OK] {inquiry.InquiryNumber}");
                        else
                        {
                            var err = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"[EMAIL FAIL] {response.StatusCode} – {err}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EMAIL EXCEPTION] {ex.Message}");
                        // neblokujeme uložení
                    }
                }

                return Results.Ok();
            })
            .WithName("UpdateInquiry");

            return app;
        }
    }
}