using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System;

namespace RestAPI1.Endpoints;

public static class Status
{
    public static IEndpointRouteBuilder MapApiStatus(this IEndpointRouteBuilder app, int rulesCount)
    {
        app.MapGet("/ApiStatus", (IHostEnvironment env) =>
        {
            try
            {
                var status = new
                {
                    Project = "RestAPI – HDPE Atributy + CRM integrace",
                    Port = env.ContentRootPath.Contains("5004") ? 5004 : 5005, // nebo lepší způsob získání portu
                    RulesCount = rulesCount,
                    CustomersTable = "StwPh.AD",
                    CustomerIDType = "int",
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Environment = env.EnvironmentName,
                    Framework = ".NET " + Environment.Version.ToString()
                };

                Console.WriteLine($"[/api/status] Stav aplikace načten – RulesCount: {rulesCount}");

                return Results.Json(status);
            }
            catch (Exception ex)
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("CHYBA v endpointu GET /api/status");
                Console.WriteLine($"Čas: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Zpráva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("=====================================");

                return Results.Problem(
                    detail: "Došlo k chybě při načítání statusu aplikace.",
                    statusCode: 500,
                    title: "Interní chyba serveru"
                );
            }
        })
        .WithName("ApiStatus");

        return app;
    }
}