using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace RestAPI1.Endpoints
{
    public static class PohodaSalesOrderEndpoints
    {
        public static IEndpointRouteBuilder MapOrderCreationInPohoda(this IEndpointRouteBuilder app)
        {
            app.MapPost("/OrderCreationInPohoda", async (BomDb db, IConfiguration config, [FromBody] CreateSalesOrderRequest req) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(req.QuoteNumber))
                        return Results.BadRequest(new { success = false, message = "QuoteNumber je povinné." });

                    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.QuoteNumber == req.QuoteNumber);
                    if (quote == null)
                    {
                        Console.WriteLine($"[CreateSalesOrder] Nabídka {req.QuoteNumber} nenalezena.");
                        return Results.NotFound(new { success = false, message = $"Nabídka {req.QuoteNumber} neexistuje." });
                    }

                    Console.WriteLine($"[CreateSalesOrder] Vytvářím přijatou objednávku (receivedOrder) z nabídky {req.QuoteNumber}");
                    Console.WriteLine($"Přijato {req.Items?.Count ?? 0} položek");

                    string xml = BuildPohodaOrderXml(req);

                    using var client = new HttpClient();

                    string credentialsBase64 = config["Pohoda:BasicAuth"]
                        ?? throw new InvalidOperationException("Chybí konfigurace 'Pohoda:BasicAuth'.");
                    string pohodaUrl = config["Pohoda:BaseUrl"]
                        ?? throw new InvalidOperationException("Chybí konfigurace 'Pohoda:BaseUrl'.");

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentialsBase64);
                    client.DefaultRequestHeaders.Add("STW-Authorization", $"Basic {credentialsBase64}");
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd("HDPE CRM 2026");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                    var content = new StringContent(xml, Encoding.GetEncoding("Windows-1250"), "application/xml");

                    var response = await client.PostAsync(pohodaUrl, content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"[POHODA ORDER] Status: {(int)response.StatusCode} {response.StatusCode}, Response length: {responseText.Length}");

                    bool isPohodaStateError = responseText.Contains("state=\"error\"", StringComparison.OrdinalIgnoreCase);
                    bool isSchemaValidationError = responseText.Contains("Nepoda", StringComparison.OrdinalIgnoreCase)
                                                  && responseText.Contains("validace", StringComparison.OrdinalIgnoreCase);

                    if (!response.IsSuccessStatusCode || isPohodaStateError)
                    {
                        Console.WriteLine($"[POHODA ERROR] Detail: {responseText}");

                        return Results.BadRequest(new
                        {
                            success = false,
                            message = isSchemaValidationError
                                ? "Pohoda odmítla XML kvůli validaci podle schématu (XSD)."
                                : "Pohoda vrátila chybu.",
                            detail = responseText
                        });
                    }

                    string? producedOrderNumber = TryExtractOrderNumber(responseText);
                    producedOrderNumber ??= $"AUTO-{req.QuoteNumber}";

                    return Results.Ok(new
                    {
                        success = true,
                        orderNumber = producedOrderNumber,
                        message = "Přijatá objednávka úspěšně vytvořena v Pohodě",
                        pohodaResponsePreview = responseText.Length > 2000 ? responseText.Substring(0, 2000) + "..." : responseText
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=====================================");
                    Console.WriteLine("CHYBA v endpointu POST /OrderCreationInPohoda");
                    Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Zpráva: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine("=====================================");

                    return Results.Problem(
                        detail: "Došlo k chybě při vytváření přijaté objednávky v Pohodě.",
                        statusCode: 500,
                        title: "Interní chyba serveru"
                    );
                }
            })
            .WithName("OrderCreationInPohoda");

            return app;
        }

        private static string BuildPohodaOrderXml(CreateSalesOrderRequest req)
        {
            static string E(string? s) => System.Security.SecurityElement.Escape(s ?? string.Empty) ?? string.Empty;

            // Číselné formáty – vždy s tečkou (InvariantCulture)
            static string D1(decimal v) => v.ToString("0.0", CultureInfo.InvariantCulture);
            static string M2(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

            DateTime docDate = req.Date ?? DateTime.Today;

            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""Windows-1250""?>");

            sb.AppendLine($@"
<dat:dataPack
  xmlns:dat=""http://www.stormware.cz/schema/version_2/data.xsd""
  xmlns:ord=""http://www.stormware.cz/schema/version_2/order.xsd""
  xmlns:typ=""http://www.stormware.cz/schema/version_2/type.xsd""
  version=""2.0""
  id=""HDPE-IMPORT-{E(req.QuoteNumber)}""
  ico=""05307970""
  application=""HDPE CRM 2026""
  note=""Import nabídky {E(req.QuoteNumber)}"">");

            sb.AppendLine($@"
  <dat:dataPackItem version=""2.0"" id=""IMPORT-{E(req.QuoteNumber)}"" actionType=""add"">
    <ord:order version=""2.0"">

      <ord:orderHeader>
        <ord:orderType>receivedOrder</ord:orderType>

        <ord:date>{docDate:yyyy-MM-dd}</ord:date>

        <ord:partnerIdentity>
          <typ:address>
            <typ:company>{E(req.CustomerName ?? "Neznámý zákazník")}</typ:company>
          </typ:address>
        </ord:partnerIdentity>

        <ord:myIdentity>
          <typ:address>
            <typ:company>Staroplastic s.r.o.</typ:company>
            <typ:name>CZ05307970</typ:name>
            <typ:city>Brno</typ:city>
            <typ:street>Vídenská 149/125a</typ:street>
            <typ:zip>619 00</typ:zip>
            <typ:ico>05307970</typ:ico>
            <typ:dic>CZ05307970</typ:dic>
            <typ:phone>770682077</typ:phone>
            <typ:email>jj@staroplastic.cz</typ:email>
            <typ:www>www.staroplastic.cz</typ:www>
          </typ:address>
          <typ:establishment>
            <typ:company>Staroplastic s.r.o.</typ:company>
            <typ:city>Brno</typ:city>
            <typ:street>Vídenská 149/125a</typ:street>
            <typ:zip>619 00</typ:zip>
          </typ:establishment>
        </ord:myIdentity>

        <ord:paymentType>
          <typ:ids>30 days</typ:ids>
        </ord:paymentType>

        <ord:isExecuted>false</ord:isExecuted>
        <ord:isDelivered>false</ord:isDelivered>

        <ord:note>Automaticky vytvořeno z HDPE CRM&#10;Zákazník: {E(req.CustomerName ?? "Neznámý")}&#10;Datum: {docDate:dd.MM.yyyy}</ord:note>
      </ord:orderHeader>

      <ord:orderDetail>");

            foreach (var item in req.Items ?? new List<OrderItemDto>())
            {
                var code = E(item.Code);
                var text = E(item.Name ?? item.Code);

                decimal qty = item.Quantity <= 0 ? 1 : item.Quantity;
                decimal unitPrice = item.SellingPrice > 0 ? item.SellingPrice : 0m; // ← pokud není cena, 0
                decimal lineTotal = unitPrice * qty;

                // Logování pro kontrolu
                Console.WriteLine($"[Order Item] Code: {item.Code}, Qty: {qty}, UnitPrice: {unitPrice}, Total: {lineTotal}");

                sb.AppendLine($@"
        <ord:orderItem>
          <ord:text>{text}</ord:text>
          <ord:quantity>{D1(qty)}</ord:quantity>
          <ord:unit>ks</ord:unit>
          <ord:coefficient>1.0</ord:coefficient>
          <ord:payVAT>false</ord:payVAT>
          <ord:rateVAT>none</ord:rateVAT>
          <ord:discountPercentage>0.0</ord:discountPercentage>

          <ord:homeCurrency>
            <typ:unitPrice>{M2(unitPrice)}</typ:unitPrice>           <!-- TADY JE CENA ZA KS -->
            <typ:price>{M2(lineTotal)}</typ:price>                   <!-- Celková cena bez DPH -->
            <typ:priceVAT>0</typ:priceVAT>
            <typ:priceSum>{M2(lineTotal)}</typ:priceSum>             <!-- Celková cena -->
          </ord:homeCurrency>

          <ord:code>{code}</ord:code>

          <ord:stockItem>
            <typ:store>
              <typ:ids>02 OUT</typ:ids>
            </typ:store>
            <typ:stockItem>
              <typ:ids>{code}</typ:ids>
            </typ:stockItem>
            <typ:serialNumber>{E(req.QuoteNumber)}</typ:serialNumber>
          </ord:stockItem>

          <ord:PDP>false</ord:PDP>
        </ord:orderItem>");
            }

            sb.AppendLine(@"
      </ord:orderDetail>

    </ord:order>
  </dat:dataPackItem>
</dat:dataPack>");

            return sb.ToString();
        }

        private static string? TryExtractOrderNumber(string responseXml)
        {
            var m1 = Regex.Match(responseXml, @"<\w*:numberRequested>([^<]+)</\w*:numberRequested>", RegexOptions.IgnoreCase);
            if (m1.Success) return m1.Groups[1].Value.Trim();

            var m2 = Regex.Match(responseXml, @"<\w*:number>([^<]+)</\w*:number>", RegexOptions.IgnoreCase);
            if (m2.Success) return m2.Groups[1].Value.Trim();

            var m3 = Regex.Match(responseXml, @"<\w*:valueProduced>([^<]+)</\w*:valueProduced>", RegexOptions.IgnoreCase);
            if (m3.Success) return m3.Groups[1].Value.Trim();

            return null;
        }
    }

    // DTOs – beze změny, jen pro přehled
    public class CreateSalesOrderRequest
    {
        public string QuoteNumber { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? Date { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }  // ← tady přichází cena za kus
    }
}