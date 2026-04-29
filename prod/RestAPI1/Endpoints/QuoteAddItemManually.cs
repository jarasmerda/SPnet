using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace RestAPI1.Endpoints
{
    public static class QuoteQuickAppendEndpoints
    {
        public static IEndpointRouteBuilder MapQuoteAddItemManually(this IEndpointRouteBuilder app)
        {
            app.MapPost("/quote/{number}/AddItemManually", async (BomDb db, string number, HttpContext ctx) =>
            {
                try
                {
                    ctx.Response.Headers.Append("Cache-Control", "no-store, no-cache");

                    // 1. Načteme quote hned na začátku (před jakýmkoliv parsováním)
                    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteNumber == number);
                    bool isNew = quote == null;

                    if (isNew)
                    {
                        quote = new QuoteHeader
                        {
                            QuoteNumber = number,
                            Status = "WORKING ON IT",
                            CreatedDate = DateTime.Now,
                            LastSaved = DateTime.Now,
                            QuoteDate = DateTime.Today,
                            ValidUntil = DateTime.Today.AddMonths(1)
                        };
                        db.Quotes.Add(quote);
                        await db.SaveChangesAsync();
                    }

                    // 2. Logujeme RAW tělo requestu
                    ctx.Request.EnableBuffering();
                    using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
                    var rawBody = await reader.ReadToEndAsync();
                    ctx.Request.Body.Position = 0;

                    Console.WriteLine("=====================================");
                    Console.WriteLine($"[AddItemManually] RAW REQUEST BODY (quote: {number}):");
                    Console.WriteLine(string.IsNullOrWhiteSpace(rawBody) ? "(prázdné nebo žádné tělo)" : rawBody);
                    Console.WriteLine("=====================================");

                    // 3. Standardní deserializace
                    QuickAppendRequest request = null;
                    try
                    {
                        request = await ctx.Request.ReadFromJsonAsync<QuickAppendRequest>();
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"[AddItemManually] CHYBA DESERIALIZACE JSON: {jsonEx.Message}");
                    }

                    // 4. Log deserializovaného requestu
                    Console.WriteLine("[AddItemManually] Deserializovaný request:");
                    if (request == null)
                    {
                        Console.WriteLine("→ request je null – deserializace selhala");
                    }
                    else
                    {
                        Console.WriteLine($"→ Status: {(request.Status ?? "(null)")}");
                        if (request.Item == null)
                        {
                            Console.WriteLine("→ Item je null");
                        }
                        else
                        {
                            Console.WriteLine($"→ Code: {request.Item.Code ?? "(null)"}");
                            Console.WriteLine($"→ Name: {request.Item.Name ?? "(null)"}");
                            Console.WriteLine($"→ Attr1: {request.Item.Attr1 ?? "(null)"}");
                            Console.WriteLine($"→ Attr2: {request.Item.Attr2 ?? "(null)"}");
                            Console.WriteLine($"→ Attr3: {request.Item.Attr3 ?? "(null)"}");
                            Console.WriteLine($"→ Attr4: {request.Item.Attr4 ?? "(null)"}");
                            Console.WriteLine($"→ Attr5: {request.Item.Attr5 ?? "(null)"}");
                            Console.WriteLine($"→ Attr6: {request.Item.Attr6 ?? "(null)"}");
                            Console.WriteLine($"→ Attr7: {request.Item.Attr7 ?? "(null)"}");
                            Console.WriteLine($"→ Quantity: {request.Item.Quantity}");
                        }
                    }

                    // 5. Fallback ruční parsování, pokud deserializace selhala
                    QuoteItem newItemFromFallback = null;
                    if (request == null || request.Item == null)
                    {
                        Console.WriteLine("[AddItemManually] Pokus o fallback ruční parsování...");
                        try
                        {
                            using var doc = JsonDocument.Parse(rawBody);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("item", out var itemElem))
                            {
                                var code = itemElem.TryGetProperty("Code", out var c) ? c.GetString() : "???";
                                var name = itemElem.TryGetProperty("Name", out var n) ? n.GetString() : "Bez názvu";
                                var attr1 = itemElem.TryGetProperty("Attr1", out var a1) ? a1.GetString() : null;
                                var attr2 = itemElem.TryGetProperty("Attr2", out var a2) ? a2.GetString() : null;
                                var attr3 = itemElem.TryGetProperty("Attr3", out var a3) ? a3.GetString() : null;
                                var attr4 = itemElem.TryGetProperty("Attr4", out var a4) ? a4.GetString() : null;
                                var attr5 = itemElem.TryGetProperty("Attr5", out var a5) ? a5.GetString() : null;
                                var attr6 = itemElem.TryGetProperty("Attr6", out var a6) ? a6.GetString() : null;
                                var attr7 = itemElem.TryGetProperty("Attr7", out var a7) ? a7.GetString() : null;
                                var quantity = itemElem.TryGetProperty("Quantity", out var q) && q.TryGetInt32(out int qty) ? qty : 1;

                                Console.WriteLine("Fallback úspěšný – použity tyto hodnoty:");
                                Console.WriteLine($"Code: {code}");
                                Console.WriteLine($"Attr1: {attr1 ?? "(null)"}");
                                Console.WriteLine($"Attr2: {attr2 ?? "(null)"}");
                                // ... atd.

                                newItemFromFallback = new QuoteItem
                                {
                                    QuoteID = quote.QuoteID,
                                    Code = code,
                                    Name = name,
                                    Attr1 = attr1,
                                    Attr2 = attr2,
                                    Attr3 = attr3,
                                    Attr4 = attr4,
                                    Attr5 = attr5,
                                    Attr6 = attr6,
                                    Attr7 = attr7,
                                    Quantity = quantity > 0 ? quantity : 1,
                                    CostPrice = 0m,
                                    SellingPrice = 0m
                                };
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"Fallback parsování selhalo: {fallbackEx.Message}");
                        }
                    }

                    // 6. Rozhodnutí, který item použít
                    QuoteItem finalItem;
                    if (newItemFromFallback != null)
                    {
                        finalItem = newItemFromFallback;
                        Console.WriteLine("[AddItemManually] Použit fallback item");
                    }
                    else if (request?.Item != null)
                    {
                        finalItem = new QuoteItem
                        {
                            QuoteID = quote.QuoteID,
                            Code = request.Item.Code ?? "???",
                            Name = request.Item.Name ?? "Bez názvu",
                            Attr1 = request.Item.Attr1,
                            Attr2 = request.Item.Attr2,
                            Attr3 = request.Item.Attr3,
                            Attr4 = request.Item.Attr4,
                            Attr5 = request.Item.Attr5,
                            Attr6 = request.Item.Attr6,
                            Attr7 = request.Item.Attr7,
                            Quantity = request.Item.Quantity > 0 ? request.Item.Quantity : 1,
                            CostPrice = 0m,
                            SellingPrice = 0m
                        };
                        Console.WriteLine("[AddItemManually] Použit deserializovaný item");
                    }
                    else
                    {
                        return Results.BadRequest(new 
                        { 
                            success = false, 
                            message = "Neplatný formát požadavku – Item chybí nebo nelze zpracovat" 
                        });
                    }

                    // 7. Uložení
                    db.QuoteItems.Add(finalItem);
                    quote.LastSaved = DateTime.Now;

                    if (!string.IsNullOrWhiteSpace(request?.Status))
                        quote.Status = request.Status;

                    await db.SaveChangesAsync();

                    Console.WriteLine($"[AddItemManually] ÚSPĚCH – uložena položka ID={finalItem.ItemID}, Attr1={finalItem.Attr1 ?? "(null)"}");

                    return Results.Ok(new
                    {
                        success = true,
                        itemId = finalItem.ItemID,
                        lastSaved = quote.LastSaved.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=====================================");
                    Console.WriteLine($"CHYBA v endpointu POST /quote/{{number}}/AddItemManually (number = {number})");
                    Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Zpráva: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=====================================");

                    return Results.Problem(
                        detail: "Došlo k chybě při přidávání položky do nabídky.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("QuoteAddItemManually");

            return app;
        }
    }
}