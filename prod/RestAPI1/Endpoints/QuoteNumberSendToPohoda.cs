using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml;
using System.Security;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using RestAPI1.Models;   // BomDb, Quote, QuoteItem, ...

namespace RestAPI1.Endpoints
{
    public static class QuoteNumberSendToPohoda
    {
        public static IEndpointRouteBuilder MapQuoteNumberSendToPohoda(this IEndpointRouteBuilder app)
        {
            
            // POST /quote/{number}/send-to-pohoda
            app.MapPost("/quote/{number}/send-to-pohoda", async (BomDb db, string number) =>
            {
                var quote = await db.Quotes
                    .FirstOrDefaultAsync(q => q.QuoteNumber == number);

                if (quote == null)
                {
                    return Results.Json(new { success = false, message = "Nabídka nenalezena" }, statusCode: 404);
                }

                if (string.IsNullOrWhiteSpace(quote.Status) ||
                    !quote.Status.Trim().Equals("WAITING FOR APPROVAL", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Json(new { success = false, message = "Nabídka musí být ve stavu 'WAITING FOR APPROVAL'" }, statusCode: 400);
                }

                try
                {
                    var items = await db.QuoteItems
                        .Where(i => i.QuoteID == quote.QuoteID)
                        .ToListAsync();

                    if (!items.Any())
                    {
                        return Results.Json(new { success = false, message = "Nabídka nemá žádné položky" }, statusCode: 400);
                    }

                    // fallback na 531 pokud není CustomerID
                    string customerIdentifier = quote.CustomerID ?? "915";

                    var inv = CultureInfo.InvariantCulture;

                    // ────────────────────────────────────────────────
                    // XML – opraveno podle poslední odpovědi Pohody
                    // ────────────────────────────────────────────────
                    var xmlBuilder = new StringBuilder();
                    xmlBuilder.Append($@"<?xml version=""1.0"" encoding=""Windows-1250""?>
<dat:dataPack 
    xmlns:dat=""http://www.stormware.cz/schema/version_2/data.xsd""
    xmlns:typ=""http://www.stormware.cz/schema/version_2/type.xsd""
    xmlns:ofr=""http://www.stormware.cz/schema/version_2/offer.xsd""
    version=""2.0""
    id=""HDPE-IMPORT-FINAL""
    ico=""05307970""
    application=""HDPE CRM 2026""
    note=""Import nabídky {SecurityElement.Escape(quote.QuoteNumber)}"">

  <dat:dataPackItem 
      version=""2.0"" 
      id=""IMPORT-{quote.QuoteNumber.Replace("-", "").Replace(" ", "").ToUpper()}""
      actionType=""add"">

    <ofr:offer version=""2.0"">

      <ofr:offerHeader>
        <ofr:offerType>issuedOffer</ofr:offerType>

        <ofr:extId>
          <typ:exSystemName>HDPE_CRM</typ:exSystemName>
          <typ:ids>{SecurityElement.Escape(quote.QuoteNumber)}</typ:ids>
        </ofr:extId>

        <ofr:date>{DateTime.Today:yyyy-MM-dd}</ofr:date>

        <ofr:partnerIdentity>
          <!-- KLÍČOVÁ OPRAVA: jednoznačný identifikátor odběratele -->
          <typ:id>{SecurityElement.Escape(customerIdentifier)}</typ:id>

          <!-- volitelně i adresa – pokud máš další údaje, přidej je sem -->
          <typ:address>
            <typ:company>{SecurityElement.Escape(quote.CustomerName ?? "Není vyplněn")}</typ:company>
          </typ:address>
        </ofr:partnerIdentity>

        <ofr:text>Cenová nabídka č. {SecurityElement.Escape(quote.QuoteNumber)}</ofr:text>

        <ofr:note>Automaticky vytvořeno z HDPE CRM&#10;Zákazník: {SecurityElement.Escape(quote.CustomerName ?? "Není vyplněn")}&#10;Datum: {DateTime.Today:yyyy-MM-dd}&#10;Platnost do: {quote.ValidUntil?.ToString("yyyy-MM-dd") ?? "Není uvedeno"}</ofr:note>

        <!-- Opravená forma úhrady – bez diakritiky, spolehlivá varianta -->
        <ofr:paymentType>
          <typ:ids>Převodem</typ:ids>
        </ofr:paymentType>

        <ofr:isExecuted>false</ofr:isExecuted>
        <ofr:isDelivered>false</ofr:isDelivered>
      </ofr:offerHeader>

      <ofr:offerDetail>");

                    foreach (var item in items)
                    {
                        string safeText = SecurityElement.Escape(item.Name ?? "Bez názvu");
                        string safeCode = SecurityElement.Escape(item.Code ?? safeText);

                        string quantityStr  = item.Quantity.ToString("F1", inv);
                        string unitPriceStr = item.SellingPrice.ToString("F2", inv);
                        string priceStr     = item.SellingPrice.ToString("F2", inv);
                        string sumStr       = (item.SellingPrice * item.Quantity).ToString("F2", inv);

                        xmlBuilder.Append($@"
        <ofr:offerItem>
          <ofr:text>{safeText}</ofr:text>
          <ofr:code>{safeCode}</ofr:code>
          <ofr:quantity>{quantityStr}</ofr:quantity>
          <ofr:unit>ks</ofr:unit>
          <ofr:payVAT>false</ofr:payVAT>
          <ofr:rateVAT>none</ofr:rateVAT>
          <ofr:discountPercentage>0.0</ofr:discountPercentage>

          <ofr:homeCurrency>
            <typ:unitPrice>{unitPriceStr}</typ:unitPrice>
            <typ:price>{priceStr}</typ:price>
            <typ:priceVAT>0</typ:priceVAT>
            <typ:priceSum>{sumStr}</typ:priceSum>
          </ofr:homeCurrency>
        </ofr:offerItem>");
                    }

                    xmlBuilder.Append(@"
      </ofr:offerDetail>

    </ofr:offer>
  </dat:dataPackItem>
</dat:dataPack>");

                    string xml = xmlBuilder.ToString();

                    // ────────────────────────────────────────────────
                    // Odeslání
                    // ────────────────────────────────────────────────
                    using var client = new HttpClient();
                    string credentialsBase64 = "YWRtaW46dWZydTc2ZG4=";

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentialsBase64);
                    client.DefaultRequestHeaders.Add("STW-Authorization", $"Basic {credentialsBase64}");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("HDPE CRM 2026");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                    var content = new StringContent(xml, Encoding.GetEncoding("Windows-1250"), "application/xml");
                    string pohodaUrl = "http://185.219.164.45:444/xml";

                    var response = await client.PostAsync(pohodaUrl, content);
                    string responseText = await response.Content.ReadAsStringAsync();

                    // Log
                    Console.WriteLine($"[Pohoda {number}] Status: {response.StatusCode}");
                    Console.WriteLine($"[Pohoda {number}] Response length: {responseText.Length}");
                    if (responseText.Length < 4000)
                        Console.WriteLine($"[Pohoda {number}] Response: {responseText}");
                    else
                        Console.WriteLine($"[Pohoda {number}] Response (začátek): {responseText.Substring(0, 2000)}...");

                    quote.PohodaImportDate = DateTime.Now;
                    quote.PohodaImportStatus = response.IsSuccessStatusCode ? "Úspěch" : $"Chyba HTTP {response.StatusCode}";
                    quote.PohodaLastResponse = responseText.Length > 8000 ? responseText.Substring(0, 8000) + "..." : responseText;

                    bool importSuccess = false;
                    string? pohodaNumber = null;

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var doc = new XmlDocument();
                            doc.LoadXml(responseText);

                            var nsmgr = new XmlNamespaceManager(doc.NameTable);
                            nsmgr.AddNamespace("rsp", "http://www.stormware.cz/schema/version_2/response.xsd");
                            nsmgr.AddNamespace("rdc", "http://www.stormware.cz/schema/version_2/documentresponse.xsd");
                            nsmgr.AddNamespace("typ", "http://www.stormware.cz/schema/version_2/type.xsd");

                            // celkový stav
                            var packState = doc.SelectSingleNode("/rsp:responsePack/@state", nsmgr)?.Value?.ToLowerInvariant();

                            // stav položky
                            var itemStateNode = doc.SelectSingleNode("//rsp:responsePackItem/@state", nsmgr);
                            string? itemState = itemStateNode?.Value?.ToLowerInvariant();

                            importSuccess = packState == "ok" && itemState == "ok";

                            // číslo nabídky
                            var numberNode = doc.SelectSingleNode("//rdc:number", nsmgr);
                            pohodaNumber = numberNode?.InnerText?.Trim();

                            if (string.IsNullOrEmpty(pohodaNumber))
                            {
                                var regexMatch = Regex.Match(responseText, @"<rdc:number[^>]*>([^<]+)</rdc:number>", RegexOptions.IgnoreCase);
                                if (regexMatch.Success) pohodaNumber = regexMatch.Groups[1].Value.Trim();
                            }
                        }
                        catch (XmlException xmlEx)
                        {
                            Console.WriteLine($"[Pohoda {number}] XML parse chyba: {xmlEx.Message}");
                            importSuccess = responseText.Contains("state=\"ok\"", StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    if (importSuccess)
                    {
                        quote.Status = "APPROVED";
                        quote.PohodaOfferNumber = pohodaNumber ?? "detekováno bez čísla";
                    }

                    await db.SaveChangesAsync();

                    return Results.Json(new 
                    {
                        success = importSuccess,
                        message = importSuccess 
                            ? "OFFER LOADED INTO POHODA" 
                            : "Import proběhl, ale obsahuje chyby/warningy – viz odpověď",
                        pohodaNumber = pohodaNumber,
                        pohodaStatus = quote.PohodaImportStatus,
                        responsePreview = responseText.Length < 800 ? responseText : "(dlouhá odpověď – viz log)"
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CRITICAL ERROR {number}] {ex.Message}\n{ex.StackTrace}");
                    return Results.Json(new { success = false, message = "Chyba serveru", detail = ex.Message }, statusCode: 500);
                }
            });

            return app;
        }
    }
}