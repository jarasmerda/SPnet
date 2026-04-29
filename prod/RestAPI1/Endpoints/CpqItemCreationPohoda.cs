using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RestAPI1.Endpoints;

public static class PohodaCreate
{
    public static IEndpointRouteBuilder MapCpqItemCreationPohoda(this IEndpointRouteBuilder app)
    {
        app.MapPost("/CpqItemCreationPohoda", async (HttpContext context) =>
        {
            try
            {
                // Načtení příchozího XML v kódování Windows-1250
                var requestBody = await new StreamReader(context.Request.Body, Encoding.GetEncoding("windows-1250"))
                    .ReadToEndAsync();

                Console.WriteLine($"[Příchozí XML z CPQ]:\n{requestBody}");

                using var client = new HttpClient();

                string credentialsBase64 = "YWRtaW46dWZydTc2ZG4=";

                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", credentialsBase64);

                client.DefaultRequestHeaders.Add("STW-Authorization", $"Basic {credentialsBase64}");
                client.DefaultRequestHeaders.UserAgent.TryParseAdd("HDPE CPQ 2026");
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/xml"));

                var content = new StringContent(
                    requestBody,
                    Encoding.GetEncoding("windows-1250"),
                    "application/xml");

                var response = await client.PostAsync("http://SRV-TERMINAL:444/xml", content);

                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var responseText = Encoding.GetEncoding("windows-1250").GetString(responseBytes);

                Console.WriteLine($"[Pohoda mServer] Status: {response.StatusCode}");
                Console.WriteLine($"[Pohoda mServer] ResponsePack: {responseText}");

                return Results.Content(
                    responseText,
                    "application/xml",
                    Encoding.GetEncoding("windows-1250"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("CHYBA v endpointu POST /create-pohoda");
                Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Zpráva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
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