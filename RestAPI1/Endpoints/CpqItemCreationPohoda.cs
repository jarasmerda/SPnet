using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RestAPI1.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RestAPI1.Endpoints;

public static class PohodaCreate
{
    public static IEndpointRouteBuilder MapCpqItemCreationPohoda(this IEndpointRouteBuilder app)
    {
        app.MapPost("/CpqItemCreationPohoda", async (HttpContext context, IConfiguration config) =>
        {
            try
            {
                var requestBody = await new StreamReader(context.Request.Body, Encoding.GetEncoding("windows-1250"))
                    .ReadToEndAsync();

                Console.WriteLine($"[Příchozí XML z CPQ]:\n{requestBody}");

                // Parsování vstupního XML — kód a atributy
                string? newCode = null;
                string? attr1 = null, attr2 = null, attr3 = null, attr4 = null,
                        attr5 = null, attr6 = null, attr7 = null;

                try
                {
                    var inDoc = new XmlDocument();
                    inDoc.LoadXml(requestBody);
                    var ns = new XmlNamespaceManager(inDoc.NameTable);
                    ns.AddNamespace("stk", "http://www.stormware.cz/schema/version_2/stock.xsd");
                    ns.AddNamespace("typ", "http://www.stormware.cz/schema/version_2/type.xsd");

                    newCode = inDoc.SelectSingleNode("//stk:code", ns)?.InnerText?.Trim();

                    var paramNodes = inDoc.SelectNodes("//typ:parameter", ns);
                    if (paramNodes != null) foreach (XmlNode param in paramNodes)
                    {
                        var name = param.SelectSingleNode("typ:name", ns)?.InnerText?.Trim();
                        var val  = param.SelectSingleNode("typ:textValue", ns)?.InnerText?.Trim();
                        switch (name)
                        {
                            case "VPrAttr1": attr1 = val; break;
                            case "VPrAttr2": attr2 = val; break;
                            case "VPrAttr3": attr3 = val; break;
                            case "VPrAttr4": attr4 = val; break;
                            case "VPrAttr5": attr5 = val; break;
                            case "VPrAttr6": attr6 = val; break;
                            case "VPrAttr7": attr7 = val; break;
                        }
                    }
                }
                catch (Exception xmlEx)
                {
                    Console.WriteLine($"[CPQ] Varování: nepodařilo se parsovat vstupní XML: {xmlEx.Message}");
                }

                // Odeslání do Pohoda mServer
                using var client = new HttpClient();
                string credentialsBase64 = config["Pohoda:BasicAuth"] ?? "YWRtaW46dWZydTc2ZG4=";
                string pohodaUrl = config["Pohoda:BaseUrl"] ?? "http://SRV-TERMINAL:444/xml";

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentialsBase64);
                client.DefaultRequestHeaders.Add("STW-Authorization", $"Basic {credentialsBase64}");
                client.DefaultRequestHeaders.UserAgent.TryParseAdd("HDPE CPQ 2026");
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/xml"));

                var content = new StringContent(requestBody, Encoding.GetEncoding("windows-1250"), "application/xml");
                var response = await client.PostAsync(pohodaUrl, content);

                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var responseText = Encoding.GetEncoding("windows-1250").GetString(responseBytes);

                Console.WriteLine($"[Pohoda mServer] Status: {response.StatusCode}");
                Console.WriteLine($"[Pohoda mServer] ResponsePack: {responseText}");

                // Kontrola úspěchu v XML odpovědi
                bool pohodaOk = false;
                try
                {
                    var respDoc = new XmlDocument();
                    respDoc.LoadXml(responseText);
                    var rns = new XmlNamespaceManager(respDoc.NameTable);
                    rns.AddNamespace("rsp", "http://www.stormware.cz/schema/version_2/response.xsd");

                    var packState = respDoc.SelectSingleNode("/rsp:responsePack/@state", rns)?.Value;
                    var itemNode  = respDoc.SelectSingleNode("//rsp:responsePackItem", rns);
                    var itemState = itemNode?.Attributes?["state"]?.Value;

                    pohodaOk = string.Equals(packState, "ok", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(itemState, "ok", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    pohodaOk = responseText.Contains("state=\"ok\"", StringComparison.OrdinalIgnoreCase)
                            && !responseText.Contains("state=\"error\"", StringComparison.OrdinalIgnoreCase);
                }

                // Po úspěšném vytvoření v Pohodě zaregistrujeme kód v trackeru,
                // aby CpqNextCode nevydal duplicitu (SKz se synchronizuje se zpožděním)
                if (pohodaOk && !string.IsNullOrWhiteSpace(newCode))
                {
                    IssuedCodesTracker.Register(newCode);
                }

                return Results.Content(responseText, "application/xml", Encoding.GetEncoding("windows-1250"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("CHYBA v endpointu POST /CpqItemCreationPohoda");
                Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Zpráva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine("=====================================");

                return Results.Problem(
                    detail: "Došlo k chybě při vytváření karty v Pohodě. Zkuste to později.",
                    statusCode: 500,
                    title: "Interní chyba serveru"
                );
            }
        })
        .WithName("CpqItemCreationPohoda");

        return app;
    }
}
