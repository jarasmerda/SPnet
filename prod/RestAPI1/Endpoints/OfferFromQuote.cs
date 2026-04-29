using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace RestAPI1.Endpoints
{
    public static class OfferFromQuote
    {
        public static IEndpointRouteBuilder MapOfferFromQuote(this IEndpointRouteBuilder app)
        {
            // ✅ CORS jen pro tento endpoint (preflight)
            app.MapMethods("/offer/from-quote", new[] { "OPTIONS" }, (HttpContext ctx) =>
            {
                ctx.Response.Headers["Access-Control-Allow-Origin"] = "http://185.219.164.45:5127";
                ctx.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
                ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
                return Results.Ok();
            })
            .WithName("OfferFromQuotePreflight");

            // POST /offer/from-quote
            // Body:
            // {
            //   "quoteNumber":"Q260003",
            //   "baseCode":"S0027",
            //   "attr8":"03",
            //   "attr9":"05"
            // }
            app.MapPost("/offer/from-quote", async (BomDb db, HttpContext context) =>
            {
                // ✅ CORS hlavička i na reálné odpovědi
                context.Response.Headers["Access-Control-Allow-Origin"] = "http://185.219.164.45:5127";

                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] POST /offer/from-quote voláno");
                Console.WriteLine($"Remote IP:          {context.Connection.RemoteIpAddress?.ToString() ?? "neznámá"}");
                Console.WriteLine($"User-Agent:         {context.Request.Headers["User-Agent"].ToString() ?? "není"}");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");

                try
                {
                    // ---- Read body ----
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (string.IsNullOrWhiteSpace(body))
                        return Results.BadRequest(new { created = false, exists = false, error = "Prázdné body" });

                    JsonElement json;
                    try
                    {
                        json = JsonSerializer.Deserialize<JsonElement>(body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[JSON PARSE ERROR] {ex.Message}");
                        return Results.BadRequest(new { created = false, exists = false, error = "Neplatný JSON" });
                    }

                    string quoteNumber = json.TryGetProperty("quoteNumber", out var qn) ? (qn.GetString() ?? "") : "";
                    string baseCode = json.TryGetProperty("baseCode", out var bc) ? (bc.GetString() ?? "") : "";
                    string attr8 = json.TryGetProperty("attr8", out var a8) ? (a8.GetString() ?? "") : "";
                    string attr9 = json.TryGetProperty("attr9", out var a9) ? (a9.GetString() ?? "") : "";

                    if (string.IsNullOrWhiteSpace(quoteNumber) ||
                        string.IsNullOrWhiteSpace(baseCode) ||
                        string.IsNullOrWhiteSpace(attr8) ||
                        string.IsNullOrWhiteSpace(attr9))
                    {
                        return Results.BadRequest(new
                        {
                            created = false,
                            exists = false,
                            error = "Chybí quoteNumber / baseCode / attr8 / attr9"
                        });
                    }

                    string newCode = $"{baseCode}-{attr8}-{attr9}";

                    // ---- Load quote + item ----
                    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteNumber == quoteNumber);
                    if (quote == null)
                        return Results.NotFound(new { created = false, exists = false, error = $"Quote {quoteNumber} nenalezena" });

                    var item = await db.QuoteItems.FirstOrDefaultAsync(i => i.QuoteID == quote.QuoteID && i.Code == baseCode);
                    if (item == null)
                        return Results.NotFound(new { created = false, exists = false, error = $"Položka {baseCode} nenalezena v quote {quoteNumber}" });

                    // ---- 1) Check existence in Pohoda (volá tvůj /check-offer endpoint) ----
                    try
                    {
                        using var checkClient = new HttpClient();
                        var checkRes = await checkClient.GetAsync($"http://185.219.164.45:5005/check-offer?code={Uri.EscapeDataString(newCode)}");
                        if (checkRes.IsSuccessStatusCode)
                        {
                            var checkText = await checkRes.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(checkText) &&
                                checkText.Contains("\"exists\": true", StringComparison.OrdinalIgnoreCase))
                            {
                                return Results.Json(new
                                {
                                    exists = true,
                                    created = false,
                                    code = newCode,
                                    message = "Kód existuje"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // check fail neblokuje tvorbu, jen zalogujeme
                        Console.WriteLine($"[CHECK-OFFER WARNING] {ex.Message}");
                    }

                    // ---- 2) Build XML – STEJNÁ STRUKTURA jako funkční create stock (S0027) + attr8/9 ----
                    string Esc(string? s) => SecurityElement.Escape(s ?? "") ?? "";

                    var parameters = new StringBuilder();

                    // RefVPrStockCat jako první (listValueRef)
                    parameters.Append(@"
  <typ:parameter>
    <typ:name>RefVPrStockCat</typ:name>
    <typ:listValueRef>
      <typ:ids>SHEET PE</typ:ids>
    </typ:listValueRef>
    <typ:list>
      <typ:ids>StockCat</typ:ids>
    </typ:list>
  </typ:parameter>");

                    void AddVPrAttr(int n, string? val)
                    {
                        // zapíšeme VPrAttr1..9 (i prázdné), ať Pohoda dostane všech 9 atributů
                        parameters.Append($@"
  <typ:parameter>
    <typ:name>VPrAttr{n}</typ:name>
    <typ:textValue>{Esc(val)}</typ:textValue>
  </typ:parameter>");
                    }

                    AddVPrAttr(1, item.Attr1);
                    AddVPrAttr(2, item.Attr2);
                    AddVPrAttr(3, item.Attr3);
                    AddVPrAttr(4, item.Attr4);
                    AddVPrAttr(5, item.Attr5);
                    AddVPrAttr(6, item.Attr6);
                    AddVPrAttr(7, item.Attr7);
                    AddVPrAttr(8, attr8); // INNER COLOR
                    AddVPrAttr(9, attr9); // OUTER COLOR

                    string cardName = (item.Name ?? newCode).Trim();
                    if (string.IsNullOrWhiteSpace(cardName)) cardName = newCode;

                    string desc =
$@"Offer generator z quote:
Quote: {quoteNumber}
Base: {baseCode}
New code: {newCode}

Attr8 (inner): {attr8}
Attr9 (outer): {attr9}";

                    string xml = $@"<?xml version=""1.0"" encoding=""Windows-1250""?>
<dat:dataPack xmlns:dat=""http://www.stormware.cz/schema/version_2/data.xsd""
              xmlns:stk=""http://www.stormware.cz/schema/version_2/stock.xsd""
              xmlns:typ=""http://www.stormware.cz/schema/version_2/type.xsd""
              version=""2.0""
              id=""OFFER01""
              ico=""05307970""
              application=""HDPE CRM 2026""
              note=""Auto create {Esc(newCode)}"">
  <dat:dataPackItem version=""2.0"" id=""{Esc(newCode)}"" actionType=""add"">
    <stk:stock version=""2.1"">
      <stk:stockHeader>
        <stk:stockType>card</stk:stockType>
        <stk:code>{Esc(newCode)}</stk:code>
        <stk:name>{Esc(cardName)}</stk:name>
        <stk:unit>ks</stk:unit>
        <stk:storage><typ:ids>02 OUT</typ:ids></stk:storage>
        <stk:typePrice><typ:ids>SK</typ:ids></stk:typePrice>
        <stk:purchasingRateVAT>high</stk:purchasingRateVAT>
        <stk:sellingRateVAT>high</stk:sellingRateVAT>
        <stk:isSales>true</stk:isSales>
        <stk:description>{Esc(desc)}</stk:description>
        <stk:parameters>
{parameters}
        </stk:parameters>
      </stk:stockHeader>
    </stk:stock>
  </dat:dataPackItem>
</dat:dataPack>";

                    // ---- 3) Send to Pohoda ----
                    using var client = new HttpClient();
                    string credentialsBase64 = "YWRtaW46dWZydTc2ZG4=";

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentialsBase64);
                    client.DefaultRequestHeaders.Add("STW-Authorization", $"Basic {credentialsBase64}");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("HDPE CRM 2026");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                    var content = new StringContent(xml, Encoding.GetEncoding("Windows-1250"), "application/xml");
                    var response = await client.PostAsync("http://185.219.164.45:444/xml", content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"[POHODA CREATE {newCode}] HTTP: {response.StatusCode} len={responseText.Length}");
                    if (responseText.Length < 4000) Console.WriteLine(responseText);
                    else Console.WriteLine(responseText.Substring(0, 2000) + "...");

                    // ---- 4) Evaluate response (created=true jen když packState ok + itemState ok) ----
                    bool created = false;
                    string? packState = null;
                    string? itemState = null;
                    string? note = null;

                    try
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(responseText);

                        var nsmgr = new XmlNamespaceManager(doc.NameTable);
                        nsmgr.AddNamespace("rsp", "http://www.stormware.cz/schema/version_2/response.xsd");

                        packState = doc.SelectSingleNode("/rsp:responsePack/@state", nsmgr)?.Value;

                        var itemNode = doc.SelectSingleNode("//rsp:responsePackItem", nsmgr);
                        itemState = itemNode?.Attributes?["state"]?.Value;
                        note = itemNode?.Attributes?["note"]?.Value;

                        created = string.Equals(packState, "ok", StringComparison.OrdinalIgnoreCase)
                               && string.Equals(itemState, "ok", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        // fallback
                        created = responseText.Contains("state=\"ok\"", StringComparison.OrdinalIgnoreCase)
                               && !responseText.Contains("state=\"error\"", StringComparison.OrdinalIgnoreCase);
                    }

                    return Results.Json(new
                    {
                        exists = false,
                        created,
                        code = newCode,
                        packState,
                        itemState,
                        note,
                        responsePreview = responseText.Length > 2000 ? responseText.Substring(0, 2000) + "..." : responseText
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=====================================");
                    Console.WriteLine("CHYBA v endpointu POST /offer/from-quote");
                    Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Zpráva: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=====================================");

                    return Results.Problem(
                        detail: "Došlo k chybě při vytváření kódu v Pohodě.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("OfferFromQuote");

            return app;
        }
    }
}
