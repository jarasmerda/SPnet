using Microsoft.AspNetCore.Builder;
using System.Text;
using System.Net.Http.Headers;
using System.Security;
using System.Xml;

namespace RestAPI1.Endpoints
{
    public static class CheckOffer
    {
        public static IEndpointRouteBuilder MapcheckOffer(this IEndpointRouteBuilder app)
        {
            app.MapGet("/check-offer", async (string code) =>
            {
                try
                {
                    using var client = new HttpClient();

                    string credentialsBase64 = "YWRtaW46dWZydTc2ZG4=";

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", credentialsBase64);
                    client.DefaultRequestHeaders.Add("STW-Authorization", $"Basic {credentialsBase64}");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("HDPE CRM 2026");
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/xml"));

                    string xml = $@"<?xml version=""1.0"" encoding=""Windows-1250""?>
<dat:dataPack
  xmlns:dat=""http://www.stormware.cz/schema/version_2/data.xsd""
  xmlns:ftr=""http://www.stormware.cz/schema/version_2/filter.xsd""
  xmlns:typ=""http://www.stormware.cz/schema/version_2/type.xsd""
  version=""2.0""
  id=""CHECK-{code}""
  ico=""05307970""
  application=""HDPE CRM 2026""
  note=""Check stock {code}"">

  <dat:dataPackItem version=""2.0"" id=""FTR-{code}"" actionType=""get"">

    <ftr:filter>
      <ftr:filterName>stock</ftr:filterName>

      <ftr:criteria>
        <ftr:filterField>code</ftr:filterField>
        <ftr:operator>equals</ftr:operator>
        <ftr:value>{SecurityElement.Escape(code)}</ftr:value>
      </ftr:criteria>

    </ftr:filter>

  </dat:dataPackItem>
</dat:dataPack>";

                    var content = new StringContent(xml, Encoding.GetEncoding("Windows-1250"), "application/xml");
                    var response = await client.PostAsync("http://185.219.164.45:444/xml", content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    bool exists = false;

                    var doc = new XmlDocument();
                    doc.LoadXml(responseText);

                    var ns = new XmlNamespaceManager(doc.NameTable);
                    ns.AddNamespace("rdc", "http://www.stormware.cz/schema/version_2/documentresponse.xsd");

                    var stockNode = doc.SelectSingleNode("//rdc:stock", ns);
                    if (stockNode != null) exists = true;

                    return Results.Json(new
                    {
                        exists,
                        code
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CHYBA /check-offer:");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);

                    return Results.Json(new
                    {
                        exists = false,
                        error = ex.Message
                    });
                }
            });

            return app;
        }
    }
}
